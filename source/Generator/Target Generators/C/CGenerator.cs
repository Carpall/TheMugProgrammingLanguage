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
/*        private CModuleBuilder Module { get; }
        private CFunctionBuilder CurrenFunctionBuilder { get; set; }
        private Stack<string> VirtualStack { get; } = new();

        private MIRFunction CurrentFunction { get; set; }

        private uint _temporaryElementCounter = 0;
        private uint _registerCounter = 0;

        private const uint VIRTUAL_REGISTERS_MAX_COUNT = 10;

        private void LowerFunction(MIRFunction function)
        {
            CurrentFunction = function;
            CurrenFunctionBuilder = new(BuildCFunctionPrototype());

            // AllocateVirtualRegisters();
            EmitAllocations();
            LowerFunctionBody();

            Module.Functions.Add(CurrenFunctionBuilder);
        }

        *//*private void AllocateVirtualRegisters()
        {
            for (int i = 0; i < VIRTUAL_REGISTERS_MAX_COUNT; i++)
                EmitStatement($"register uint64 R{i}");
        }*//*

        private void EmitAllocations()
        {
            for (int i = CurrentFunction.ParameterTypes.Length; i < CurrentFunction.Allocations.Length; i++)
                EmitStatement($"{CurrentFunction.Allocations[i].Type} _{i}");
        }

        private void LowerFunctionBody()
        {
            foreach (var block in CurrentFunction.Body)
                EmitBlock(block);
        }

        private void EmitBlock(MIRBlock block)
        {
            foreach (var instruction in block.Instructions)
                EmitLoweredInstruction(instruction);
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
                case MIRInstructionKind.JumpFalse: EmitJumpFalse(instruction); break;
                case MIRInstructionKind.Dupplicate: EmitDupplicate(); break;
                case MIRInstructionKind.Greater: EmitGreater(instruction); break;
                case MIRInstructionKind.Less: EmitLess(instruction); break;
                case MIRInstructionKind.LoadZeroinitialized: EmitLoadZeroinitialized(instruction); break;
                case MIRInstructionKind.LoadValueFromPointer: EmitLoadFromPointer(instruction); break;
                case MIRInstructionKind.LoadField: EmitLoadField(instruction); break;
                case MIRInstructionKind.StoreField: EmitStoreField(instruction); break;
                case MIRInstructionKind.StoreGlobal: EmitStoreGlobal(instruction); break;
                case MIRInstructionKind.CastIntToInt: break;
                default:
                    CompilationTower.Todo($"implement {instruction.Kind} in {nameof(EmitLoweredInstruction)}");
                    break;
            }
        }

        private void EmitStoreGlobal(MIRInstruction instruction)
        {
            EmitStatement($"{instruction.GetName()} =", PopValue());
        }

        private void EmitStoreField(MIRInstruction instruction)
        {
            var expression = PopValue();
            EmitStatement($"{PopValue()}.{GetLocal(instruction.GetStackIndex())} =", expression);
        }

        private void EmitLoadField(MIRInstruction instruction)
        {
            PushValue($"{PopValue()}.{GetLocal(instruction.GetStackIndex())}", instruction.Type);
        }

        private void EmitLoadFromPointer(MIRInstruction instruction)
        {
            EmitUnaryOperation("*", instruction.Type);
        }

        private void EmitLoadZeroinitialized(MIRInstruction instruction)
        {
            var temp = GetTempName();
            EmitStatement($"{instruction.Type} {temp}");
            PushValue(temp, instruction.Type);
        }

        private void EmitLess(MIRInstruction instruction)
        {
            EmitOperation("<", instruction.Type);
        }

        private void EmitGreater(MIRInstruction instruction)
        {
            EmitOperation(">", instruction.Type);
        }

        private void EmitLabel(MIRInstruction instruction)
        {
            EmitStatement($"L{instruction.GetStackIndex()}", ":");
        }

        private void EmitDupplicate()
        {
            VirtualStack.Push(VirtualStack.Peek());
        }

        private void EmitJump(MIRInstruction instruction)
        {
            EmitStatement($"goto", BuildLabel(instruction.GetLabel().BodyIndex));
            // ResetVirtualStackCounter();
        }

        private void EmitJumpFalse(MIRInstruction instruction)
        {
            EmitStatement($"if (!({PopValue()})) goto", BuildLabel(instruction.GetLabel().BodyIndex));
        }

        *//*private void ResetVirtualStackCounter()
        {
            _registerCounter = 0;
        }*//*

        private static string BuildLabel(int bodyIndex)
        {
            return $"L{bodyIndex}";
        }

        private void EmitCall(MIRInstruction instruction)
        {
            var isNotVoid = !instruction.Type.IsVoid();
            var call = $"{BuildFunctionName(instruction.GetName())}({ string.Join(", ", PopFunctionArgs(instruction.GetName()))})";

            if (isNotVoid)
                PushValue(call, instruction.Type);
            else
                EmitStatement(call);
        }

        private string[] PopFunctionArgs(string functionName)
        {
            var count = Tower.MIRModule.GetFunction(functionName).ParameterTypes.Length;
            var result = new string[count];
            while (count-- > 0)
                result[count] = PopValue();

            return result;
        }

        private string GetTempName()
        {
            return $"atmp_{GetAndIncTmpCounter()}";
        }

        private uint GetAndIncTmpCounter()
        {
            return _temporaryElementCounter++;
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
            PushValue($"{op} {PopValue()}", type);
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
            PushValue($"{PopValue()} {op} {right}", type);
        }

        private void EmitLoadLocal(MIRInstruction instruction)
        {
            PushValue(GetLocal(instruction.GetStackIndex()), instruction.Type);
        }

        private static string GetLocal(int stackindex)
        {
            return $"_{stackindex}";
        }

        private void EmitStoreLocal(MIRInstruction instruction)
        {
            EmitStatement($"{GetLocal(instruction.GetStackIndex())} =", PopValue());
        }

        private void EmitReturn(MIRInstruction instruction)
        {
            EmitStatement("return", !instruction.Type.IsVoid() ? PopValue() : "");
        }

        private void EmitLoadConstant(MIRInstruction instruction)
        {
            PushValue(MIRConstantToCConstant(instruction), instruction.Type);
        }

        private static string MIRConstantToCConstant(MIRInstruction instruction)
        {
            return instruction.Type.Kind switch
            {
                MIRTypeKind.Int or MIRTypeKind.UInt => instruction.ConstantIntValue.ToString()
            };
        }
        
        private void PushValue(string expression, MIRType type)
        {
            var reg = _registerCounter++;
            MovInVirtualReg(reg, expression, type);
            VirtualStack.Push($"R{reg}");
        }

        private string PopValue()
        {
            return VirtualStack.Pop();
        }

        private void EmitStatement(string statement, string expression = "")
        {
            CurrenFunctionBuilder.Body.AppendLine($"    {statement} {expression};");
        }

        private void MovInVirtualReg(uint index, string expression, MIRType type)
        {
            EmitStatement($"const {type} R{index} =", expression);
        }

        private string BuildCFunctionPrototype()
        {
            return $"{CurrentFunction.ReturnType} {BuildFunctionName(CurrentFunction.Name)}({BuildParametersInCFunctionPrototypes(CurrentFunction.ParameterTypes)})";
        }

        private static string BuildFunctionName(string name)
        {
            return $"mug__{name}";
        }

        private static string BuildParametersInCFunctionPrototypes(MIRType[] parameterTypes)
        {
            var result = new StringBuilder();
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (i > 0)
                    result.Append(", ");

                result.Append($"{parameterTypes[i]} _{i}");
            }

            return result.ToString();
        }

        */override public object Lower()
        {
            return null;
            /*LowerStructures();
            LowerGlobals();
            LowerFunctions();
            GenerateMain();

            return Module.Build();*/
        }/*

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
            var entrypointBuilder = new CFunctionBuilder("int main()");
            entrypointBuilder.Body.Append("    return mug__main();");

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
            var structureBuilder = new CStructureBuilder($"struct {structure.Name} {(structure.IsPacked ? "__attribute__((__packed__))" : "")}");
            for (int i = 0; i < structure.Body.Length; i++)
                structureBuilder.Body.Add($"    {structure.Body[i]} _{i};");

            Module.Structures.Add(structureBuilder);
        }*/

        public CGenerator(CompilationTower tower) : base(tower)
        {
            // Module = new(Tower.OutputFilename);
        }
    }
}
