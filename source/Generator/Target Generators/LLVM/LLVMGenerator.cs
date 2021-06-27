using LLVMSharp.Interop;
using Mug.Compilation;
using Mug.Generator.IR;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using Mug.Parser.AST.Statements;
using Mug.Parser.AST;
using Mug.Generator.TargetGenerators;

using LLModule = LLVMSharp.Interop.LLVMModuleRef;
using LLType = LLVMSharp.Interop.LLVMTypeRef;
using LLValue = LLVMSharp.Interop.LLVMValueRef;
using LLBuilder = LLVMSharp.Interop.LLVMBuilderRef;
using LLBlock = LLVMSharp.Interop.LLVMBasicBlockRef;
using LLVMC = LLVMSharp.Interop.LLVM;

namespace Mug.Generator.TargetGenerators.LLVM
{
    public class LLVMGenerator : TargetGenerator
    {
        public LLModule Module { get; }

        private MIRFunction CurrentFunction { get; set; }
        private LLValue CurrentLLVMFunction { get; set; }
        private LLBuilder CurrentFunctionBuilder { get; set; }
        private (LLValue Value, MIRAllocationAttribute Attributes)[] Allocations { get; set; }
        private MIRBlock CurrentBlock { get; set; }

        private Stack<LLValue> StackValues { get; } = new();

        private readonly List<LLValue> _stackBlocksHistory = new();

        public LLVMGenerator(CompilationTower tower) : base(tower)
        {
            Module = LLModule.CreateWithName(tower.OutputFilename);
        }

        private void DeclareFunctionPrototypes()
        {
            foreach (var functionPrototype in Tower.MIRModule.FunctionPrototypes)
                DeclareFunctionPrototype(functionPrototype);

            foreach (var function in Tower.MIRModule.Functions)
                DeclareFunctionPrototype(function.Prototype);
        }

        private void WalkThroughFunctions()
        {
            foreach (var function in Tower.MIRModule.Functions)
                LowerFunction(function);
        }

        private void DeclareFunctionPrototype(MIRFunctionPrototype function)
        {
            Module.AddFunction(function.Name, CreateLLVMFunctionModel(function));
        }

        private LLType LowerDataType(DataType type)
        {
            var kind = type.SolvedType.Kind;
            if (kind >= (TypeKind)4 && kind <= (TypeKind)33)
                return LLType.CreateInt(((uint)kind - Convert.ToUInt32((int)kind % 2 == 1)) * 2);

            return kind switch
            {
                TypeKind.Void => LLType.Void,
                TypeKind.Bool => LLType.Int1,
                TypeKind.CustomType => Module.GetTypeByName(type.SolvedType.GetStruct().Name),
                TypeKind.Pointer => LLType.CreatePointer(LowerDataType(type.SolvedType.GetBaseElementType()), 0),
                TypeKind.Option => LowerOption(type),
                TypeKind.Enum => LowerDataType(type.SolvedType.GetEnum().BaseType),
                TypeKind.EmptyEnum => LLType.Int1,
                _ => ToImplement<LLType>(kind.ToString(), nameof(LowerDataType))
            };
        }

        private LLType LowerOption(DataType type)
        {
            var option = type.SolvedType.GetOption();
            return LLType.CreateStruct(new[] { LowerDataType(option.Error), LowerDataType(option.Success) }, true);
        }

        private void LowerStruct(MIRStructure type)
        {
            unsafe
            {
                var lltype = LLVMC.StructCreateNamed(Module.Context, new SByteString(type.Name));
                LLVMC.StructSetBody(
                    lltype,
                    ArrayToPointer(LowerDataTypes(type.Body)),
                    (uint)type.Body.Length,
                    Convert.ToInt32(type.IsPacked));
            }
        }

        private static bool IsUnsafeNull(LLType value)
        {
            return value.Handle == IntPtr.Zero;
        }

        private static unsafe LLVMOpaqueType** ArrayToPointer(LLType[] types)
        {
            fixed (LLType* fixedTypes = types)
                return (LLVMOpaqueType**)fixedTypes;
        }

        private LLType[] LowerDataTypes(DataType[] types)
        {
            var results = new LLType[types.Length];
            for (int i = 0; i < types.Length; i++)
                results[i] = LowerDataType(types[i]);

            return results;
        }

        private static T ToImplement<T>(string what, string where)
        {
            CompilationTower.Todo($"implement {what} in {where}");
            return default;
        }

        private void LowerFunction(MIRFunction function)
        {
            CurrentFunction = function;
            CurrentLLVMFunction = GetLLVMFunction(CurrentFunction.Prototype.Name);
            Allocations = new (LLValue, MIRAllocationAttribute)[function.Allocations.Length];
            
            LowerFunctionBody();
        }

        private void EmitAllocationsAndAssignParameters()
        {
            EmitAllocations();
            EmitParametersAssign();
        }

        private void EmitParametersAssign()
        {
            for (uint i = 0; i < CurrentFunction.Prototype.ParameterTypes.Length; i++)
                Allocations[i].Value = CurrentLLVMFunction.GetParam(i);
        }

        private void LowerFunctionBody()
        {
            DeclareAllBlocks();
            LowerAllBlocks();
        }

        private void LowerAllBlocks()
        {
            for (int i = 0; i < CurrentFunction.Body.Length; i++)
            {
                CurrentBlock = CurrentFunction.Body[i];

                CreateBuilder(CurrentBlock);

                if (i == 0)
                    EmitAllocationsAndAssignParameters();

                LowerBlock(CurrentBlock);
            }
        }

        private void CreateBuilder(MIRBlock block)
        {
            CurrentFunctionBuilder = LLBuilder.Create(Module.Context);
            CurrentFunctionBuilder.PositionAtEnd(GetBlock(block.Index));
        }

        private void DeclareAllBlocks()
        {
            for (int i = 0; i < CurrentFunction.Body.Length; i++)
                CurrentLLVMFunction.AppendBasicBlock(CurrentFunction.Body[i].Identifier);
        }

        private void LowerBlock(MIRBlock block)
        {
            foreach (var instruction in block.Instructions)
                EmitLoweredInstruction(instruction);

            SaveLastStackValueIfNeededAndClear();
        }

        private void SaveLastStackValueIfNeededAndClear()
        {
            _stackBlocksHistory.Add(
                StackValues.Count == 1 ?
                    StackValuesPop() :
                    new());
        }

        private void EmitLoweredInstruction(MIRInstruction instruction)
        {
            switch (instruction.Kind)
            {
                case MIRInstructionKind.Return:               EmitReturn(instruction);              break;
                case MIRInstructionKind.Load:                 EmitLoadConstant(instruction);        break;
                case MIRInstructionKind.StoreLocal:           EmitStoreLocal(instruction);          break;
                case MIRInstructionKind.LoadLocal:            EmitLoadLocal(instruction);           break;
                case MIRInstructionKind.Dupplicate:           EmitDupplicate();                     break;
                case MIRInstructionKind.LoadZeroinitialized:  EmitLoadZeroinitialized(instruction); break;
                case MIRInstructionKind.LoadValueFromPointer: EmitLoadFromPointer();                break;
                case MIRInstructionKind.Add:                  EmitAdd(instruction);                 break;
                case MIRInstructionKind.Sub:                  EmitSub(instruction);                 break;
                case MIRInstructionKind.Mul:                  EmitMul(instruction);                 break;
                case MIRInstructionKind.Div:                  EmitDiv(instruction);                 break;
                case MIRInstructionKind.Call:                 EmitCall(instruction);                break;
                case MIRInstructionKind.Pop:                  EmitPop();                            break;
                case MIRInstructionKind.Neg:                  EmitNeg(instruction);                 break;
                case MIRInstructionKind.Ceq:                  EmitCeq(instruction);                 break;
                case MIRInstructionKind.Neq:                  EmitNeq(instruction);                 break;
                case MIRInstructionKind.Geq:                  EmitGeq(instruction);                 break;
                case MIRInstructionKind.Leq:                  EmitLeq(instruction);                 break;
                case MIRInstructionKind.Greater:              EmitGreater(instruction);             break;
                case MIRInstructionKind.Less:                 EmitLess(instruction);                break;
                case MIRInstructionKind.Jump:                 EmitJump(instruction);                break;
                case MIRInstructionKind.JumpConditional:      EmitJumpFalse(instruction);           break;
                case MIRInstructionKind.LoadField:            EmitLoadField(instruction);           break;
                case MIRInstructionKind.StoreField:           EmitStoreField(instruction);          break;
                case MIRInstructionKind.StoreGlobal:          EmitStoreGlobal(instruction);         break;
                case MIRInstructionKind.CastIntToInt:         EmitCastIntToInt(instruction);        break;
                case MIRInstructionKind.LoadLocalAddress:     EmitLoadLocalAddress(instruction);    break;
                case MIRInstructionKind.LoadFieldAddress:     EmitLoadFieldAddress(instruction);    break;
                case MIRInstructionKind.CastPointerToPointer: EmitCastPointerToPointer(instruction); break;
                case MIRInstructionKind.StorePointer:         EmitStorePointer(); break;
                default:
                    ToImplement<object>(instruction.Kind.ToString(), nameof(EmitLoweredInstruction));
                    break;
            }
        }

        private void EmitStorePointer()
        {
            var value = StackValuesPop();
            var pointer = PointerCast(StackValuesPop(), value.TypeOf);
            CurrentFunctionBuilder.BuildStore(value, pointer);
        }

        private LLValue PointerCast(LLValue pointer, LLType valuetype)
        {
            if (pointer.TypeOf.ElementType != valuetype)
                pointer = CurrentFunctionBuilder.BuildBitCast(pointer, LLType.CreatePointer(valuetype, 0));

            return pointer;
        }

        private void EmitCastPointerToPointer(MIRInstruction instruction)
        {
            StackValuesPush(CurrentFunctionBuilder.BuildBitCast(StackValuesPop(), LowerDataType(instruction.Type)));
        }

        private void EmitLoadFieldAddress(MIRInstruction instruction)
        {
            StackValuesPush(CurrentFunctionBuilder.BuildStructGEP(StackValuesPop(), (uint)instruction.GetInt()));
        }

        private void EmitLoadLocalAddress(MIRInstruction instruction)
        {
            StackValuesPush(Allocations[instruction.GetInt()].Value);
        }

        private void EmitCastIntToInt(MIRInstruction instruction)
        {
            var type = LowerDataType(instruction.Type);
            StackValuesPush(CurrentFunctionBuilder.BuildIntCast(StackValuesPop(), type));
        }

        private void EmitStoreGlobal(MIRInstruction instruction)
        {
            var global = Module.GetNamedGlobal(instruction.GetName());
            var value = StackValuesPop();
            CurrentFunctionBuilder.BuildStore(value, global);
        }

        private void EmitStoreField(MIRInstruction instruction)
        {
            var value = StackValuesPop();
            var instance = StackValuesPop();
            var index = (uint)instruction.GetInt();
            var changedStruct = CurrentFunctionBuilder.BuildInsertValue(instance, value, index);

            StackValuesPush(changedStruct);
        }

        private void EmitLoadField(MIRInstruction instruction)
        {
            var instance = StackValuesPop();
            var index = (uint)instruction.GetInt();
            StackValuesPush(CurrentFunctionBuilder.BuildExtractValue(instance, index));
        }

        private void EmitJumpFalse(MIRInstruction instruction)
        {
            var (then, otherwise) = instruction.GetConditionTuple();
            var thenBlock = GetBlock(then);
            var otherwiseBlock = GetBlock(otherwise);
            var value = StackValuesPop();

            CurrentFunctionBuilder.BuildCondBr(value, thenBlock, otherwiseBlock);
        }

        private LLBlock GetBlock(int blockIndex)
        {
            return CurrentLLVMFunction.BasicBlocks[blockIndex];
        }

        private void EmitJump(MIRInstruction instruction)
        {
            CurrentFunctionBuilder.BuildBr(GetBlock(instruction.GetInt()));
        }

        private void EmitGeq(MIRInstruction instruction)
        {
            EmitCmpSOrU(instruction.Type, LLVMIntPredicate.LLVMIntSGE, LLVMIntPredicate.LLVMIntUGE);
        }

        private void EmitLeq(MIRInstruction instruction)
        {
            EmitCmpSOrU(instruction.Type, LLVMIntPredicate.LLVMIntSLE, LLVMIntPredicate.LLVMIntULE);
        }

        private void EmitGreater(MIRInstruction instruction)
        {
            EmitCmpSOrU(instruction.Type, LLVMIntPredicate.LLVMIntSGT, LLVMIntPredicate.LLVMIntUGT);
        }

        private void EmitLess(MIRInstruction instruction)
        {
            EmitCmpSOrU(instruction.Type, LLVMIntPredicate.LLVMIntSLT, LLVMIntPredicate.LLVMIntULT);
        }

        private void EmitCmpSOrU(DataType type, LLVMIntPredicate sop, LLVMIntPredicate uop)
        {
            EmitCmp(type.SolvedType.IsSignedInt() ? sop : uop, type);
        }

        private void EmitCeq(MIRInstruction instruction)
        {
            EmitCmp(LLVMIntPredicate.LLVMIntEQ, instruction.Type);
        }

        private void EmitNeq(MIRInstruction instruction)
        {
            EmitCmp(LLVMIntPredicate.LLVMIntNE, instruction.Type);
        }

        private void EmitCmp(LLVMIntPredicate kind, DataType type)
        {
            var right = StackValuesPop();
            StackValuesPush(CurrentFunctionBuilder.BuildICmp(kind, StackValuesPop(), right));
        }

        private void EmitNegInt(MIRInstruction instruction)
        {
            var lltype = LowerDataType(instruction.Type);
            StackValuesPush(CurrentFunctionBuilder.BuildSub(CreateLLConstInt(lltype, 0), StackValuesPop()));
        }

        private void EmitNeg(MIRInstruction instruction)
        {
            if (instruction.Type.SolvedType.IsInt())
                EmitNegInt(instruction);
            else
                EmitNegBool();
        }

        private void EmitNegBool()
        {
            var value = CurrentFunctionBuilder.BuildXor(StackValuesPop(), CreateLLConstInt(LLType.Int1, 1));
            StackValuesPush(value);
        }

        private void EmitPop()
        {
            StackValuesPopTop();
        }

        private void EmitCall(MIRInstruction instruction)
        {
            var function = GetLLVMFunction(instruction.GetName());
            var result = CurrentFunctionBuilder.BuildCall(function, PopArgs(function.ParamsCount));
            StackValuesPush(result);
        }

        private LLValue[] PopArgs(uint paramsCount)
        {
            var result = new LLValue[paramsCount];
            while (paramsCount-- > 0)
                result[paramsCount] = StackValuesPop();

            return result;
        }

        private LLValue GetLLVMFunction(string name)
        {
            return Module.GetNamedFunction(name);
        }

        private void EmitDiv(MIRInstruction instruction)
        {
            var right = StackValuesPop();
            StackValuesPush(
                instruction.Type.SolvedType.IsSignedInt() ?
                    CurrentFunctionBuilder.BuildSDiv(StackValuesPop(), right) :
                    CurrentFunctionBuilder.BuildUDiv(StackValuesPop(), right));
        }

        private void EmitMul(MIRInstruction instruction)
        {
            var right = StackValuesPop();
            StackValuesPush(CurrentFunctionBuilder.BuildMul(StackValuesPop(), right));
        }

        private void EmitSub(MIRInstruction instruction)
        {
            var right = StackValuesPop();
            StackValuesPush(CurrentFunctionBuilder.BuildSub(StackValuesPop(), right));
        }

        private void EmitAdd(MIRInstruction instruction)
        {
            var right = StackValuesPop();
            StackValuesPush(CurrentFunctionBuilder.BuildAdd(StackValuesPop(), right));
        }

        private void EmitLoadFromPointer()
        {
            StackValuesPush(CurrentFunctionBuilder.BuildLoad(StackValuesPop()));
        }

        private void EmitLoadZeroinitialized(MIRInstruction instruction)
        {
            var lltype = LowerDataType(instruction.Type);
            StackValuesPush(CurrentFunctionBuilder.BuildLoad(CurrentFunctionBuilder.BuildAlloca(lltype)));
        }

        private void EmitDupplicate()
        {
            StackValuesPush(StackValues.Peek());
        }

        private void EmitLoadLocal(MIRInstruction instruction)
        {
            var stackindex = instruction.GetStackIndex();
            var (Value, Attributes) = Allocations[stackindex];

            StackValuesPush(
                Attributes is MIRAllocationAttribute.Unmutable ?
                    Value :
                    CurrentFunctionBuilder.BuildLoad(Value));
        }

        private void EmitStoreLocal(MIRInstruction instruction)
        {
            var stackindex = instruction.GetStackIndex();
            var value = StackValuesPop();

            if (IsMutableAllocation(stackindex, out var allocation))
                CurrentFunctionBuilder.BuildStore(value, allocation);
            else
                Allocations[stackindex].Value = value;
        }

        private bool IsMutableAllocation(int stackindex, out LLValue allocation)
        {
            allocation = Allocations[stackindex].Value;
            return Allocations[stackindex].Attributes != MIRAllocationAttribute.Unmutable;
        }

        private void EmitLoadConstant(MIRInstruction instruction)
        {
            var lltype = LowerDataType(instruction.Type);
            
            StackValuesPush(
                lltype.Kind is LLVMTypeKind.LLVMIntegerTypeKind ?
                    CreateLLConstInt(lltype, instruction.ConstantIntValue) :
                    MIRConstantPointerToLLVMConst(instruction));
        }

        private LLValue MIRConstantPointerToLLVMConst(MIRInstruction instruction)
        {
            return
                instruction.Value is 0L ?
                    LLValue.CreateConstNull(LowerDataType(instruction.Type)) :
                    CreateLLConstCString(instruction.GetName());
        }

        private LLValue CreateLLConstCString(string value)
        {
            return CurrentFunctionBuilder.BuildGlobalStringPtr(value);
        }

        private static LLValue CreateLLConstInt(LLType intType, long value)
        {
            var constInt = LLValue.CreateConstInt(intType, (ulong)Math.Abs(value));

            if (IsNegative(value))
                constInt = LLValue.CreateConstNeg(constInt);

            return constInt;
        }

        private static bool IsNegative(long value)
        {
            return value < 0;
        }

        private void EmitReturn(MIRInstruction instruction)
        {
            if (instruction.Type.SolvedType.IsVoid())
                CurrentFunctionBuilder.BuildRetVoid();
            else
                CurrentFunctionBuilder.BuildRet(StackValuesPop());
        }

        private LLValue StackValuesPop()
        {
            if (!StackValues.TryPop(out var value))
                value = CollectPhiNodesForCurrentBlock();

            return value;
        }

        private LLValue CollectPhiNodesForCurrentBlock()
        {
            var (blocks, labels) = CollectPhiNodesRelatedToBlock(CurrentBlock.ReferredFrom, out var type);
            var result = CurrentFunctionBuilder.BuildPhi(type);

            result.AddIncoming(labels, blocks, (uint)blocks.Length);

            return result;
        }

        private (LLBlock[] Blocks, LLValue[] Labels) CollectPhiNodesRelatedToBlock(List<int> currentBlockReferences, out LLType type)
        {
            var blocks = new List<LLBlock>();
            var labels = new List<LLValue>();
            var currentFunctionBlocks = CurrentLLVMFunction.BasicBlocks;
            type = new();

            foreach (var blockIndex in currentBlockReferences)
            {
                var currentFunctionBlock = currentFunctionBlocks[blockIndex];
                var lastInstructionInBlock = GetLastStackValueOfBlock(blockIndex);

                type = lastInstructionInBlock.TypeOf;

                blocks.Add(currentFunctionBlock);
                labels.Add(lastInstructionInBlock);
            }

            return (blocks.ToArray(), labels.ToArray());
        }

        private LLValue GetLastStackValueOfBlock(int blockIndex)
        {
            return _stackBlocksHistory[blockIndex];
        }

        private void StackValuesPopTop()
        {
            StackValues.Pop();
        }

        private void StackValuesPush(LLValue value)
        {
            StackValues.Push(value);
        }

        private LLType CreateLLVMFunctionModel(MIRFunctionPrototype function)
        {
            return
                LLType.CreateFunction(
                    LowerDataType(function.ReturnType),
                    LowerDataTypes(function.ParameterTypes));
        }

        private void EmitAllocations()
        {
            for (int i = 0; i < CurrentFunction.Allocations.Length; i++)
                BuildAllocation(CurrentFunction.Allocations[i], i);
        }

        private void BuildAllocation(MIRAllocation allocation, int index)
        {
            Allocations[index].Attributes = allocation.Attributes;

            if (allocation.Attributes is not MIRAllocationAttribute.Unmutable)
                Allocations[index].Value = CurrentFunctionBuilder.BuildAlloca(LowerDataType(allocation.Type));
        }

        private void WalkThroughStructures()
        {
            foreach (var structure in Tower.MIRModule.Structures)
                LowerStruct(structure);
        }

        public override object Lower()
        {
            WalkThroughStructures();
            DeclareFunctionPrototypes();
            WalkThroughFunctions();
            VerifyModule();

            return Module;
        }

        private void VerifyModule()
        {
            if (!Module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out var message))
                Console.WriteLine(message);
        }
    }
}
