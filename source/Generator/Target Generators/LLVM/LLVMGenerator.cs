using LLVMSharp.Interop;
using Mug.Compilation;
using Mug.Models.Generator.IR;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LLModule = LLVMSharp.Interop.LLVMModuleRef;
using LLType = LLVMSharp.Interop.LLVMTypeRef;
using LLValue = LLVMSharp.Interop.LLVMValueRef;
using LLBuilder = LLVMSharp.Interop.LLVMBuilderRef;

namespace Mug.Generator.TargetGenerators.LLVM
{
    public class LLVMGenerator : CompilerComponent
    {
        public LLModule Module { get; }
        
        private MIRFunction CurrentFunction { get; set; }
        private LLValue CurrentLLVMFunction { get; set; }
        private LLBuilder CurrentFunctionBuilder { get; set; }
        private (LLValue Value, MIRAllocationAttribute Attributes)[] Allocations { get; set; }

        private Stack<LLValue> StackValues { get; } = new();

        public LLVMGenerator(CompilationTower tower) : base(tower)
        {
            Module = LLModule.CreateWithName(tower.OutputFilename);
        }

        private void WalkFunctions()
        {
            foreach (var function in Tower.MIRModule.Functions)
                LowerFunction(function);
        }

        private LLType LowerDataType(DataType type)
        {
            return type.SolvedType.Kind switch
            {
                TypeKind.Bool => LLType.Int1,
                TypeKind.Int8 => LLType.Int8,
                TypeKind.Int16 => LLType.Int16,
                TypeKind.Int32 => LLType.Int32,
                TypeKind.Int64 => LLType.Int64,
                TypeKind.Void => LLType.Void,
                _ => ToImplement<LLType>(type.SolvedType.Kind.ToString(), nameof(LowerDataType))
            };
        }

        private static T ToImplement<T>(string what, string where)
        {
            CompilationTower.Todo($"implement {what} in {where}");
            return default;
        }

        private void LowerFunction(MIRFunction function)
        {
            CurrentFunction = function;
            CurrentLLVMFunction = Module.AddFunction(function.Name, CreateLLVMFunctionModel(function));
            CurrentFunctionBuilder = CreateBuilder();
            Allocations = new (LLValue, MIRAllocationAttribute)[function.Allocations.Length];

            EmitAllocations();
            EmitParametersAssign();
            LowerFunctionBody();
        }

        private void EmitParametersAssign()
        {
            for (uint i = 0; i < CurrentFunction.ParameterTypes.Length; i++)
                Allocations[i].Value = CurrentLLVMFunction.GetParam(i);
        }

        private void LowerFunctionBody()
        {
            foreach (var instruction in CurrentFunction.Body)
                EmitLoweredInstruction(instruction);
        }

        private void EmitLoweredInstruction(MIRInstruction instruction)
        {
            switch (instruction.Kind)
            {
                case MIRInstructionKind.Return:
                    EmitReturn(instruction);
                    break;
                case MIRInstructionKind.Load:
                    EmitLoadConstant(instruction);
                    break;
                case MIRInstructionKind.StoreLocal:
                    EmitStoreLocal(instruction);
                    break;
                case MIRInstructionKind.LoadLocal:
                    EmitLoadLocal(instruction);
                    break;
                /*case MIRInstructionKind.Dupplicate:
                    break;
                case MIRInstructionKind.LoadZeroinitialized:
                    break;
                case MIRInstructionKind.LoadField:
                    break;
                case MIRInstructionKind.StoreField:
                    break;
                case MIRInstructionKind.Comment:
                    break;
                case MIRInstructionKind.Div:
                    break;
                case MIRInstructionKind.Add:
                    break;
                case MIRInstructionKind.Sub:
                    break;
                case MIRInstructionKind.Mul:
                    break;
                case MIRInstructionKind.Call:
                    break;
                case MIRInstructionKind.Pop:
                    break;
                case MIRInstructionKind.JumpFalse:
                    break;
                case MIRInstructionKind.Jump:
                    break;
                case MIRInstructionKind.Ceq:
                    break;
                case MIRInstructionKind.Neq:
                    break;
                case MIRInstructionKind.Leq:
                    break;
                case MIRInstructionKind.Geq:
                    break;
                case MIRInstructionKind.Greater:
                    break;
                case MIRInstructionKind.Less:
                    break;*/
                default:
                    ToImplement<object>(instruction.Kind.ToString(), nameof(EmitLoweredInstruction));
                    break;
            }
        }

        private void EmitLoadLocal(MIRInstruction instruction)
        {
            var stackindex = instruction.GetStackIndex();
            var allocation = Allocations[stackindex];

            StackValuesPush(
                allocation.Attributes == MIRAllocationAttribute.Unmutable ?
                    allocation.Value :
                    CurrentFunctionBuilder.BuildLoad(allocation.Value));
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
            StackValuesPush(instruction.Type.SolvedType.Kind switch
            {
                TypeKind.Int8
                or TypeKind.Int16
                or TypeKind.Int32
                or TypeKind.Int64 => CreateLLConstInt(lltype, instruction.ConstantIntValue),
                _ => ToImplement<LLValue>(instruction.Type.SolvedType.Kind.ToString(), nameof(EmitLoadConstant))
            });
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
            return StackValues.Pop();
        }

        private void StackValuesPopTop()
        {
            StackValues.Pop();
        }

        private void StackValuesPush(LLValue value)
        {
            StackValues.Push(value);
        }

        private LLType CreateLLVMFunctionModel(MIRFunction function)
        {
            return
                LLType.CreateFunction(
                    LowerDataType(function.ReturnType),
                    LowerDataTypes(function.ParameterTypes));
        }

        private LLType[] LowerDataTypes(DataType[] parameterTypes)
        {
            var result = new LLType[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
                result[i] = LowerDataType(parameterTypes[i]);

            return result;
        }

        private LLBuilder CreateBuilder()
        {
            var builder = LLBuilder.Create(Module.Context);
            builder.PositionAtEnd(CurrentLLVMFunction.AppendBasicBlock(""));

            return builder;
        }

        private void EmitAllocations()
        {
            for (int i = 0; i < CurrentFunction.Allocations.Length; i++)
                BuildAllocation(CurrentFunction.Allocations[i], i);
        }

        private void BuildAllocation(MIRAllocation allocation, int index)
        {
            Allocations[index].Attributes = allocation.Attributes;

            if (allocation.Attributes != MIRAllocationAttribute.Unmutable)
                Allocations[index].Value = CurrentFunctionBuilder.BuildAlloca(LowerDataType(allocation.Type));
        }

        public LLModule Lower()
        {
            WalkFunctions();
            Module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);

            return Module;
        }
    }
}
