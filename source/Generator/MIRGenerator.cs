using Mug.Compilation;
using Mug.Models.Generator.IR;
using Mug.Models.Generator.IR.Builder;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.AST;
using Mug.Models.Parser.AST.Statements;
using Mug.Symbols;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;

using VirtualMemory = System.Collections.Generic.Dictionary<string, Mug.Models.Generator.AllocationData>;

namespace Mug.Models.Generator
{
    public class MIRGenerator : MugComponent
    {
        internal MIRModuleBuilder Module { get; } = new();

        private MIRFunctionBuilder FunctionBuilder { get; set; }
        private FunctionStatement CurrentFunction { get; set; }
        private VirtualMemory VirtualMemory { get; } = new();
        private Stack<MugType> ContextTypes { get; set; } = new();

        public MIRGenerator(CompilationTower tower) : base(tower)
        {
        }

        public MIR Generate()
        {
            WalkDeclarations();

            Tower.CheckDiagnostic();
            return Module.Build();
        }

        private MIRType LowerType(MugType type)
        {
            var solved = type.SolvedType.Value;

            // code to compress
            switch (solved.Kind)
            {
                case TypeKind.Array:
                case TypeKind.Pointer:
                    return new MIRType(MIRTypeKind.Pointer, LowerType(solved.Base as MugType));
                case TypeKind.Char:
                case TypeKind.Int32:
                case TypeKind.Int64:
                case TypeKind.UInt8:
                case TypeKind.UInt32:
                case TypeKind.UInt64:
                case TypeKind.Bool:
                case TypeKind.Void:
                case TypeKind.Float32:
                case TypeKind.Float64:
                case TypeKind.Float128:
                    return new MIRType(TypeKindPrimitiveToMIRTypeKind(solved.Kind));
                case TypeKind.DefinedType:
                    return new MIRType(MIRTypeKind.Struct, new MIRStruct(LowerStructType(solved.GetStruct())));
                /*case TypeKind.GenericDefinedType:
                    break;
                case TypeKind.String:
                    break;
                case TypeKind.Undefined:  
                    break;
                case TypeKind.Unknown:
                    break;
                case TypeKind.EnumError:
                    break;
                case TypeKind.Reference:
                    break;
                case TypeKind.Err:
                    break;*/
                default:
                    CompilationTower.Todo($"implement '{solved.Kind}' in MIRGenerator.LowerType");
                    return default;
            }
        }

        private MIRType[] LowerStructType(StructSymbol structsymbol)
        {
            var result = new MIRType[structsymbol.Type.Body.Count];

            for (int i = 0; i < structsymbol.Type.Body.Count; i++)
                 result[i] = LowerType(structsymbol.Type.Body[i].Type);

            return result;
        }

        private static MIRTypeKind TypeKindPrimitiveToMIRTypeKind(TypeKind kind)
        {
            return kind switch
            {
                TypeKind.Char or TypeKind.Bool or TypeKind.UInt8 => MIRTypeKind.Int8,
                TypeKind.Int32 => MIRTypeKind.Int32,
                TypeKind.UInt32 => MIRTypeKind.UInt32,
                TypeKind.Int64 => MIRTypeKind.Int64,
                TypeKind.UInt64 => MIRTypeKind.UInt64,
                TypeKind.Void => MIRTypeKind.Void,
                TypeKind.Float32 => MIRTypeKind.Float32,
                TypeKind.Float64 => MIRTypeKind.Float64,
                TypeKind.Float128 => MIRTypeKind.Float128
            };
        }

        private MIRType[] LowerParameterTypes(ParameterListNode parameters)
        {
            var result = new MIRType[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                result[i] = LowerType(parameters.Parameters[i].Type);

            return result;
        }

        private void GenerateFunctionBlock(BlockNode block)
        {
            foreach (var statement in block.Statements)
                RecognizeStatement(statement);
        }

        private void RecognizeStatement(INode opaque)
        {
            switch (opaque)
            {
                case VariableStatement statement:
                    GenerateVarStatement(statement);
                    break;
                default:
                    CompilationTower.Todo($"implement {opaque} in MIRGenerator.RecognizeStatement");
                    break;
            }
        }

        private void GenerateVarStatement(VariableStatement statement)
        {
            ContextTypes.Push(statement.Type);

            var expressiontype = EvaluateExpression(statement.Body);
            var allocation = DeclareVirtualMemorySymbol(statement.Name, FixAuto(statement.Type, expressiontype), statement.Position);

            CheckTypes(statement.Type, expressiontype, statement.Body.Position);

            FunctionBuilder.EmitStoreLocal(allocation);

            ContextTypes.Pop();
        }

        private void CheckTypes(MugType expected, MugType gottype, ModulePosition position)
        {
            var rightsolved = expected.SolvedType.Value;
            var leftsolved = gottype.SolvedType.Value;

            if (rightsolved.Kind != leftsolved.Kind)
                Tower.Report(position, $"Type mismatch: expected type '{expected}', but got '{gottype}'");
        }

        private static MugType FixAuto(MugType type, MugType expressiontype)
        {
            if (type.SolvedType.Value.Kind == TypeKind.Auto)
                type.Solve(expressiontype.SolvedType.Value);

            return type;
        }

        private MugType EvaluateExpression(INode body)
        {
            MugType type = null;

            switch (body)
            {
                case Token expression:
                    type = expression.Kind == TokenKind.Identifier ?
                        EvaluateIdentifier(expression.Value) :
                        EvaluateConstant(expression);
                    break;
                case TypeAllocationNode expression:
                    type = EvaluateTypeAllocationNode(expression);
                    break;
                default:
                    CompilationTower.Todo($"implement {body} in MIRGenerator.EvaluateExpression");
                    break;
            }

            return type;
        }

        private MugType EvaluateTypeAllocationNode(TypeAllocationNode expression)
        {
            var type = expression.Name.SolvedType.Value;

            if (!type.IsNewOperatorAllocable())
            {
                Tower.Report(expression.Position, $"Unable to allocate type '{type}' via operator 'new'");
                return expression.Name;
            }

            var result = type.GetStruct();
            var structure = result.Type;
            var assignedFields = new List<string>();

            FunctionBuilder.EmitLoadZeroinitializedStruct(LowerType(expression.Name));

            for (int i = 0; i < expression.Body.Count; i++)
            {
                var field = expression.Body[i];

                if (assignedFields.Contains(field.Name))
                {
                    Tower.Report(field.Position, $"Field '{field.Name}' assigned multiple times");
                    continue;
                }

                assignedFields.Add(field.Name);

                var fieldtype = GetFieldType(field.Name, structure.Body, out var fieldindex);
                if (fieldtype is null)
                {
                    Tower.Report(field.Position, $"Type '{structure.Name}' does not contain a definition for '{field.Name}'");
                    continue;
                }

                ContextTypes.Push(fieldtype);

                FunctionBuilder.EmitDupplicate();
                CheckTypes(fieldtype, EvaluateExpression(field.Body), field.Position);
                FunctionBuilder.EmitStoreField(MIRValue.StaticMemoryAddress(fieldindex));

                ContextTypes.Pop();
            }

            return MugType.Solved(SolvedType.Struct(result));
        }

        private static MugType GetFieldType(string name, List<FieldNode> body, out int i)
        {
            for (i = 0; i < body.Count; i++)
            {
                var field = body[i];
                if (name == field.Name)
                    return field.Type;
            }

            return null;
        }

        private MugType EvaluateIdentifier(string value)
        {
            return VirtualMemory.TryGetValue(value, out var allocation) ? allocation.Type : MugType.Solved(SolvedType.Primitive(TypeKind.Void));
        }

        private MugType EvaluateConstant(Token expression)
        {
            MugType result = null;
            MIRValue value;

            switch (expression.Kind)
            {
                case TokenKind.ConstantDigit:
                    result = CoercedOr(MugType.Solved(SolvedType.Primitive(TypeKind.Int32)));
                    value = MIRValue.Constant(LowerType(result), ulong.Parse(expression.Value));
                    break;
                default:
                    CompilationTower.Todo($"implement {expression.Kind} in MIRGenerator.EvaluateConstant");
                    value = new();
                    break;
            }

            FunctionBuilder.EmitLoadConstantValue(value);

            return result;
        }

        private MugType CoercedOr(MugType or)
        {
            var contexttype = ContextTypes.Peek();
            return contexttype.SolvedType.Value.IsInt() ? contexttype : or;
        }

        private MIRValue DeclareVirtualMemorySymbol(string name, MugType type, ModulePosition position)
        {
            var localindex = VirtualMemory.Count;
            if (!VirtualMemory.TryAdd(name, new(localindex, type)))
                Tower.Report(position, $"Variable '{name}' is already declared");

            FunctionBuilder.DeclareAllocation(LowerType(type));

            return MIRValue.StaticMemoryAddress(localindex);
        }

        private void GenerateFunction(FunctionStatement func)
        {
            FunctionBuilder = new MIRFunctionBuilder(func.Name, LowerType(func.ReturnType), LowerParameterTypes(func.ParameterList));

            CurrentFunction = func;

            GenerateFunctionBlock(func.Body);

            Module.Define(FunctionBuilder.Build());
        }

        private void WalkDeclarations()
        {
            foreach (var symbol in Tower.Symbols.GetCache())
                RecognizeSymbol(symbol.Value);
        }

        private void RecognizeSymbol(ISymbol value)
        {
            switch (value)
            {
                case StructSymbol: break;
                case FuncSymbol funcsymbol:
                    GenerateFunction(funcsymbol.Func);
                    break;
                default:
                    CompilationTower.Todo($"implement {value} in MIRGenerator.RecognizeSymbol");
                    break;
            }
        }
    }
}
