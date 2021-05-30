using Mug.Compilation;
using Mug.Models.Generator.IR;
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
        private CFunctionBuilder CurrenFunctionBuilder { get; set; }
        private Stack<string> CurrentCExpression { get; } = new();

        private MIRFunction CurrentFunction { get; set; }

        private uint _temporaryElementCounter = 0;

        private void LowerFunction(MIRFunction function)
        {
            CurrentFunction = function;
            CurrenFunctionBuilder = new(BuildCFunctionPrototype());

            EmitAllocations();
            LowerFunctionBody();

            Module.Functions.Add(CurrenFunctionBuilder);
        }

        private void EmitAllocations()
        {
            for (int i = 0; i < CurrentFunction.Allocations.Length; i++)
                EmitStatement($"{CurrentFunction.Allocations[i].Type} _{i}");
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
                case MIRInstructionKind.Label: EmitLabel(instruction); break;
                case MIRInstructionKind.Greater: EmitGreater(instruction); break;
                case MIRInstructionKind.Less: EmitLess(instruction); break;
                case MIRInstructionKind.LoadZeroinitialized: EmitLoadZeroinitialized(instruction); break;
                case MIRInstructionKind.LoadValueFromPointer: EmitLoadFromPointer(); break;
                case MIRInstructionKind.LoadField: EmitLoadField(instruction); break;
                case MIRInstructionKind.StoreField: EmitStoreField(instruction); break;
                default:
                    CompilationTower.Todo($"implement {instruction.Kind} in {nameof(EmitLoweredInstruction)}");
                    break;
            }
        }

        private void EmitStoreField(MIRInstruction instruction)
        {
            var expression = GetExpression();
            EmitStatement($"{GetExpression()}->{GetLocal(instruction.GetStackIndex())} =", expression);
        }

        private void EmitLoadField(MIRInstruction instruction)
        {
            AppendExpression($"{instruction.Type}->{GetLocal(instruction.GetStackIndex())}");
        }

        private void EmitLoadFromPointer()
        {
            EmitUnaryOperation("*");
        }

        private void EmitLoadZeroinitialized(MIRInstruction instruction)
        {
            var temp = GetTempName();
            EmitStatement($"{instruction.Type} {temp}");
            AppendExpression($"&{temp}");
        }

        private void EmitLess(MIRInstruction instruction)
        {
            EmitOperation("<");
        }

        private void EmitGreater(MIRInstruction instruction)
        {
            EmitOperation(">");
        }

        private void EmitLabel(MIRInstruction instruction)
        {
            EmitStatement($"L{instruction.GetStackIndex()}", ":");
        }

        private void EmitDupplicate()
        {
            AppendExpression(CurrentCExpression.Peek());
        }

        private void EmitJump(MIRInstruction instruction)
        {
            EmitStatement($"goto", BuildLabel(instruction.GetLabel().BodyIndex));
        }

        private void EmitJumpFalse(MIRInstruction instruction)
        {
            EmitStatement($"if (!({GetExpression()})) goto", BuildLabel(instruction.GetLabel().BodyIndex));
        }

        private static string BuildLabel(int bodyIndex)
        {
            return $"L{bodyIndex}";
        }

        private void EmitCall(MIRInstruction instruction)
        {
            var call = GetTempName();
            var isvoid = instruction.Type.IsVoid();
            if (!isvoid)
                AppendExpression(call);

            EmitStatement(
                !isvoid ?
                    $"{instruction.Type} {call} =" :
                    "", $"{instruction.GetName()}()");
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
            GetExpression();
        }

        private void EmitLeq(MIRInstruction instruction)
        {
            EmitOperation("<=");
        }

        private void EmitGeq(MIRInstruction instruction)
        {
            EmitOperation(">=");
        }

        private void EmitNeq(MIRInstruction instruction)
        {
            EmitOperation("!=");
        }

        private void EmitCeq(MIRInstruction instruction)
        {
            EmitOperation("==");
        }

        private void EmitNeg(MIRInstruction instruction)
        {
            EmitUnaryOperation("!");
        }

        private void EmitUnaryOperation(string op)
        {
            AppendExpression($"{op} {GetExpression()}");
        }

        private void EmitDiv(MIRInstruction instruction)
        {
            EmitOperation("/");
        }

        private void EmitMul(MIRInstruction instruction)
        {
            EmitOperation("*");
        }

        private void EmitSub(MIRInstruction instruction)
        {
            EmitOperation("-");
        }

        private void EmitAdd(MIRInstruction instruction)
        {
            EmitOperation("+");
        }

        private void EmitOperation(string op)
        {
            var right = CurrentCExpression.Pop();
            AppendExpression($"{CurrentCExpression.Pop()} {op} {right}");
        }

        private void EmitLoadLocal(MIRInstruction instruction)
        {
            AppendExpression(GetLocal(instruction.GetStackIndex()));
        }

        private static string GetLocal(int stackindex)
        {
            return $"_{stackindex}";
        }

        private void EmitStoreLocal(MIRInstruction instruction)
        {
            EmitStatement($"{GetLocal(instruction.GetStackIndex())} =", GetExpression());
        }

        private void EmitReturn(MIRInstruction instruction)
        {
            EmitStatement("return", !instruction.Type.IsVoid() ? GetExpression() : "");
        }

        private void EmitLoadConstant(MIRInstruction instruction)
        {
            AppendExpression(instruction.Value.ToString());
        }

        private void AppendExpression(string expression)
        {
            CurrentCExpression.Push($"({expression})");
        }

        private string GetExpression()
        {
            return CurrentCExpression.Pop();
        }

        private void EmitStatement(string statement, string expression = "")
        {
            CurrenFunctionBuilder.Body.AppendLine($"    {statement} {expression};");
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

                result.Append($"{parameterTypes[i]} p_{i}");
            }

            return result.ToString();
        }

        override public object Lower()
        {
            LowerFunctions();
            LowerStructures();

            return Module.Build();
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
        }

        public CGenerator(CompilationTower tower) : base(tower)
        {
            Module = new(Tower.OutputFilename);
        }
    }
}
