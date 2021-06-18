using Mug.Compilation;
using Mug.Generator.IR;
using Mug.Generator.TargetGenerators;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.TargetGenerators.C
{
    class CGenerator : TargetGenerator
    {
        private CModuleBuilder Module { get; }
        private CFunctionBuilder CurrentFunctionBuilder { get; set; }
        private Stack<CValue> StackValues { get; } = new();

        private MIRFunction CurrentFunction { get; set; }
        private MIRBlock CurrentBlock { get; set; }

        private int ParametersCount => CurrentFunction.Prototype.ParameterTypes.Length;

        private uint _registerCounter = 0;
        private readonly List<CValue> _stackBlocksHistory = new();

        private void LowerFunction(MIRFunction function)
        {
            ResetRegisterCounter();
            CurrentFunction = function;
            CurrentFunctionBuilder = new(BuildCFunctionPrototype(), function.Prototype.ParameterTypes.Length);

            EmitAllocations();
            LowerFunctionBody();

            Module.Functions.Add(CurrentFunctionBuilder);
        }

        private void ResetRegisterCounter()
        {
            _registerCounter = 0;
        }

        private void EmitAllocations()
        {
            for (int i = CurrentFunction.Prototype.ParameterTypes.Length; i < CurrentFunction.Allocations.Length; i++)
            {
                var allocation = CurrentFunction.Allocations[i];
                CurrentFunctionBuilder.Allocations.Add((allocation.Type, GetCAllocationAttributes(allocation.Attributes)));
            }
        }

        private static string GetCAllocationAttributes(MIRAllocationAttribute attributes)
        {
            return attributes switch
            {
                MIRAllocationAttribute.Unmutable => "register",
                _ => null
            };
        }

        private void LowerFunctionBody()
        {
            foreach (var block in CurrentFunction.Body)
            {
                CurrentBlock = block;

                EmitBlock(block);
            }
        }

        private void EmitBlock(MIRBlock block)
        {
            AddBlock();

            foreach (var instruction in block.Instructions)
                EmitLoweredInstruction(instruction);

            SaveLastStackValueIfNeededAndClear();
        }

        private void AddBlock()
        {
            CurrentFunctionBuilder.Body.Add(new());
        }

        private void SaveLastStackValueIfNeededAndClear()
        {
            _stackBlocksHistory.Add(
                StackValues.Count == 1 ?
                    PopValue() :
                    new(new(), null));
        }

        private void EmitLoweredInstruction(MIRInstruction instruction)
        {
            switch (instruction.Kind)
            {
                case MIRInstructionKind.Return: EmitReturn(instruction); break;
                case MIRInstructionKind.Load: EmitLoadConstant(instruction); break;
                case MIRInstructionKind.StoreLocal: EmitStoreLocal(instruction); break;
                case MIRInstructionKind.LoadLocal: EmitLoadLocal(instruction); break;
                case MIRInstructionKind.Add: EmitAdd(instruction); break;
                case MIRInstructionKind.Sub: EmitSub(instruction); break;
                case MIRInstructionKind.Mul: EmitMul(instruction); break;
                case MIRInstructionKind.Div: EmitDiv(instruction); break;
                case MIRInstructionKind.Neg: EmitNeg(instruction); break;
                case MIRInstructionKind.Ceq: EmitCeq(instruction); break;
                case MIRInstructionKind.Neq: EmitNeq(instruction); break;
                case MIRInstructionKind.Geq: EmitGeq(instruction); break;
                case MIRInstructionKind.Leq: EmitLeq(instruction); break;
                case MIRInstructionKind.Pop: EmitPop(); break;
                case MIRInstructionKind.Call: EmitCall(instruction); break;
                case MIRInstructionKind.Jump: EmitJump(instruction); break;
                case MIRInstructionKind.JumpConditional: EmitJumpConditional(instruction); break;
                case MIRInstructionKind.Dupplicate: EmitDupplicate(); break;
                case MIRInstructionKind.Greater: EmitGreater(instruction); break;
                case MIRInstructionKind.Less: EmitLess(instruction); break;
                case MIRInstructionKind.LoadZeroinitialized: EmitLoadZeroinitialized(instruction); break;
                case MIRInstructionKind.LoadValueFromPointer: EmitLoadFromPointer(instruction); break;
                case MIRInstructionKind.LoadField: EmitLoadField(instruction); break;
                case MIRInstructionKind.StoreField: EmitStoreField(instruction); break;
                case MIRInstructionKind.StoreGlobal: EmitStoreGlobal(instruction); break;
                case MIRInstructionKind.CastIntToInt: EmitCast(instruction); break;
                case MIRInstructionKind.LoadLocalAddress: EmitLoadLocalAddress(instruction); break;
                case MIRInstructionKind.LoadFieldAddress: EmitLoadFieldAddress(instruction); break;
                case MIRInstructionKind.CastPointerToPointer: EmitCast(instruction); break;
                case MIRInstructionKind.StorePointer: EmitStorePointer(); break;
                default:
                    CompilationTower.Todo($"implement {instruction.Kind} in {nameof(EmitLoweredInstruction)}");
                    break;
            }
        }

        private void EmitStorePointer()
        {
            var value = PopValue().Value;
            var pointer = PopValue().Value;
            EmitStatement($"*({pointer}) =", value);
        }

        private void EmitCast(MIRInstruction instruction)
        {
            LoadValue($"({instruction.Type})({PopValue()})", instruction.Type);
        }

        private void EmitLoadFieldAddress(MIRInstruction instruction)
        {
            EmitLoadField(instruction);
        }

        private void EmitLoadLocalAddress(MIRInstruction instruction)
        {
            LoadValue(GEP(GetLocal(instruction.GetStackIndex())), instruction.Type);
        }

        private static string GEP(string expression)
        {
            return $"&({expression})";
        }

        private void EmitStoreGlobal(MIRInstruction instruction)
        {
            EmitStatement($"{instruction.GetName()} =", PopValue().Value);
        }

        private void EmitStoreField(MIRInstruction instruction)
        {
            var expression = PopValue().Value;
            var instance = PopValue();
            var reg = MovInVirtualReg(instance.Value, instance.Type);

            EmitStatement($"{reg}.{GetField(instruction.GetStackIndex())} =", expression);

            LoadValue(reg, instance.Type);
        }

        private void LoadValue(string value, MIRType type)
        {
            StackValues.Push(new(type, value));
        }

        private void EmitLoadField(MIRInstruction instruction)
        {
            LoadValue($"{PopValue()}.{GetField(instruction.GetStackIndex())}", instruction.Type);
        }

        private static string GetField(int bodyIndex)
        {
            return $"F{bodyIndex}";
        }

        private void EmitLoadFromPointer(MIRInstruction instruction)
        {
            EmitUnaryOperation("*", instruction.Type);
        }

        private string CreateTempStackAllocation(MIRType type, out string reg)
        {
            reg = $"R{GetRegCountAndInc()}";
            return $"{type} {reg}";
        }

        private uint GetRegCountAndInc()
        {
            return _registerCounter++;
        }

        private void EmitLoadZeroinitialized(MIRInstruction instruction)
        {
            var allocation = CreateTempStackAllocation(instruction.Type, out var reg);
            EmitStatement(allocation);

            LoadValue(reg, instruction.Type);
        }

        private void EmitLess(MIRInstruction instruction)
        {
            EmitOperation("<", instruction.Type);
        }

        private void EmitGreater(MIRInstruction instruction)
        {
            EmitOperation(">", instruction.Type);
        }

        private void EmitDupplicate()
        {
            StackValues.Push(StackValues.Peek());
        }

        private void EmitJump(MIRInstruction instruction)
        {
            EmitStatement($"goto", BuildLabel(instruction.GetInt()));
        }

        private void EmitJumpConditional(MIRInstruction instruction)
        {
            var (then, otherwise) = instruction.GetConditionTuple();

            EmitStatement($"if ({PopValue()}) goto", BuildLabel(then));
            EmitStatement($"else goto", BuildLabel(otherwise));
        }

        private static string BuildLabel(int bodyIndex)
        {
            return $"L{bodyIndex}";
        }

        private void EmitCall(MIRInstruction instruction)
        {
            var isNotVoid = !instruction.Type.IsVoid();
            var call = BuildCallExpression(instruction.GetName());

            if (isNotVoid)
                LoadInVirtualReg(call, instruction.Type);
            else
                EmitStatement(call);
        }

        private void LoadInVirtualReg(string expression, MIRType type)
        {
            LoadValue(MovInVirtualReg(expression, type), type);
        }

        private string BuildCallExpression(string name)
        {
            var args = PopFunctionArgs(name, out var isExtern);
            return $"{(isExtern ? name : BuildFunctionName(name))}({string.Join(", ", args)})";
        }

        private string[] PopFunctionArgs(string functionName, out bool isExtern)
        {
            var count = Tower.MIRModule.GetFunction(functionName, out isExtern).ParameterTypes.Length;
            var result = new string[count];
            while (count-- > 0)
                result[count] = PopValue().Value;

            return result;
        }

        private void EmitPop()
        {
            PopValue();
        }

        private void EmitLeq(MIRInstruction instruction)
        {
            EmitOperation("<=", instruction.Type);
        }

        private void EmitGeq(MIRInstruction instruction)
        {
            EmitOperation(">=", instruction.Type);
        }

        private void EmitNeq(MIRInstruction instruction)
        {
            EmitOperation("!=", instruction.Type);
        }

        private void EmitCeq(MIRInstruction instruction)
        {
            EmitOperation("==", instruction.Type);
        }

        private void EmitNeg(MIRInstruction instruction)
        {
            EmitUnaryOperation("!", instruction.Type);
        }

        private void EmitUnaryOperation(string op, MIRType type)
        {
            LoadValue($"({op} {PopValue()})", type);
        }

        private void EmitDiv(MIRInstruction instruction)
        {
            EmitOperation("/", instruction.Type);
        }

        private void EmitMul(MIRInstruction instruction)
        {
            EmitOperation("*", instruction.Type);
        }

        private void EmitSub(MIRInstruction instruction)
        {
            EmitOperation("-", instruction.Type);
        }

        private void EmitAdd(MIRInstruction instruction)
        {
            EmitOperation("+", instruction.Type);
        }

        private void EmitOperation(string op, MIRType type)
        {
            var right = PopValue();
            LoadValue($"({PopValue()} {op} {right})", type);
        }

        private void EmitLoadLocal(MIRInstruction instruction)
        {
            var local = GetLocal(instruction.GetStackIndex());
            LoadValue(local, instruction.Type);
        }

        private static string GetLocal(int stackindex)
        {
            return $"A{stackindex}";
        }

        private void EmitStoreLocal(MIRInstruction instruction)
        {
            EmitStatement($"{GetLocal(instruction.GetStackIndex())} =", PopValue().Value);
        }

        private void EmitReturn(MIRInstruction instruction)
        {
            EmitStatement("return", !instruction.Type.IsVoid() ? PopValue().Value : null);
        }

        private void EmitLoadConstant(MIRInstruction instruction)
        {
            LoadValue(MIRConstantToCConstant(instruction), instruction.Type);
        }

        private static string MIRConstantToCConstant(MIRInstruction instruction)
        {
            return instruction.Type.Kind switch
            {
                MIRTypeKind.Int or MIRTypeKind.UInt => instruction.ConstantIntValue.ToString(),
                MIRTypeKind.Pointer => MIRPointerConstantToCConstant(instruction)
            };
        }

        private static string MIRPointerConstantToCConstant(MIRInstruction instruction)
        {
            return
                instruction.Value is 0L ?
                    "0" :
                    $"\"{instruction.Value}\"";
        }

        private CValue PopValue()
        {
            if (!StackValues.TryPop(out var value))
                value = CollectPhiNodesForCurrentBlock();

            return value;
        }

        private CValue CollectPhiNodesForCurrentBlock()
        {
            var values = CollectPhiNodesRelatedToBlock(CurrentBlock.ReferredFrom, out var type);

            var name = DeclareAllocation(type);

            EmitStoreInEachBlock(name, CurrentBlock.ReferredFrom.ToArray(), values);

            return new(type, name);
        }

        private string DeclareAllocation(MIRType type, string attributes = null)
        {
            var result = GetLocal(CurrentFunctionBuilder.Allocations.Count);
            CurrentFunctionBuilder.Allocations.Add((type, attributes));

            return result;
        }

        private void EmitStoreInEachBlock(string tempAllocationName, int[] blockIndexes, string[] values)
        {
            for (int i = 0; i < blockIndexes.Length; i++)
            {
                var blockPointer = CurrentFunctionBuilder.Body[blockIndexes[i]];
                blockPointer.Insert(blockPointer.Count - 1, $"{tempAllocationName} = {values[i]};");
            }
        }

        private string[] CollectPhiNodesRelatedToBlock(List<int> currentBlockReferences, out MIRType type)
        {
            var values = new List<string>();
            type = new();

            foreach (var blockIndex in currentBlockReferences)
            {
                var lastInstructionInBlock = GetLastStackValueOfBlock(blockIndex);

                type = lastInstructionInBlock.Type;

                values.Add(lastInstructionInBlock.Value);
            }

            return values.ToArray();
        }

        private CValue GetLastStackValueOfBlock(int blockIndex)
        {
            return _stackBlocksHistory[blockIndex];
        }

        private void EmitStatement(string statement, string expression = null)
        {
            CurrentFunctionBuilder.CurrentBlock.Add($"{statement}{(expression is not null ? $" {expression}" : "")};");
        }

        private string MovInVirtualReg(string expression, MIRType type)
        {
            var allocation = CreateTempStackAllocation(type, out var reg);
            EmitStatement($"{allocation} =", expression);

            return reg;
        }

        private string BuildCFunctionPrototype()
        {
            return $"{CurrentFunction.Prototype.ReturnType} {BuildFunctionName(CurrentFunction.Prototype.Name)}({BuildParametersInCFunctionPrototypes(CurrentFunction.Prototype.ParameterTypes)})";
        }

        private static string BuildFunctionName(string name)
        {
            return $"mug__{name.Replace('.', '_')}";
        }

        private static string BuildParametersInCFunctionPrototypes(MIRType[] parameterTypes)
        {
            var result = new StringBuilder();
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (i > 0)
                    result.Append(", ");

                result.Append($"{parameterTypes[i]} A{i}");
            }

            return result.ToString();
        }

        override public object Lower()
        {
            LowerStructures();
            LowerFunctionPrototypes();
            LowerGlobals();
            LowerFunctions();
            GenerateMain();

            return Module.Build();
        }

        private void LowerFunctionPrototypes()
        {
            foreach (var functionPrototype in Tower.MIRModule.FunctionPrototypes)
                LowerFunctionPrototype(functionPrototype);
        }

        private void LowerFunctionPrototype(MIRFunctionPrototype functionPrototype)
        {
            Module.FunctionPrototypes.Add($"{functionPrototype.ReturnType} {functionPrototype.Name}({string.Join(", ", functionPrototype.ParameterTypes)});");
        }

        private void LowerGlobals()
        {
            foreach (var global in Tower.MIRModule.Globals)
                LowerGlobal(global);
        }

        private void LowerGlobal(MIRGlobal global)
        {
            Module.Globals.Add($"{global.Type} {global.Name};");
        }

        private void GenerateMain()
        {
            var entrypointBuilder = new CFunctionBuilder("int main()", 0);
            entrypointBuilder.Body.Add(new List<string> { "mug__main();", "return 0;" });

            Module.Functions.Add(entrypointBuilder);
        }

        private void LowerFunctions()
        {
            foreach (var function in Tower.MIRModule.Functions)
                LowerFunction(function);
        }

        private void LowerStructures()
        {
            foreach (var structure in Tower.MIRModule.Structures)
                LowerStructure(structure);
        }

        private void LowerStructure(MIRStructure structure)
        {
            // {(structure.IsPacked ? "__attribute__((__packed__))" : "")}
            var structureBuilder = new CStructureBuilder($"struct {structure.Name}");
            for (int i = 0; i < structure.Body.Length; i++)
                structureBuilder.Body.Add($"    {structure.Body[i]} F{i};");

            Module.Structures.Add(structureBuilder);
        }

        public CGenerator(CompilationTower tower) : base(tower)
        {
            Module = new(Tower.OutputFilename);
        }
    }
}
