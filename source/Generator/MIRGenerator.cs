using Zap.Compilation;
using Zap.Models.Generator.IR;
using Zap.Models.Generator.IR.Builder;
using Zap.Models.Lexer;
using Zap.Models.Parser;
using Zap.Models.Parser.AST;
using Zap.Models.Parser.AST.Statements;
using Zap.Symbols;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;

using VirtualMemory = System.Collections.Generic.Dictionary<string, Zap.Models.Generator.AllocationData>;

namespace Zap.Models.Generator
{
    public class MIRGenerator : ZapComponent
    {
        public MIRModuleBuilder Module { get; } = new();

        private MIRFunctionBuilder FunctionBuilder { get; set; }
        private FunctionStatement CurrentFunction { get; set; }
        private VirtualMemory VirtualMemory { get; set; }
        private Stack<ZapType> ContextTypes { get; set; } = new();
        
        private ZapType ContextType => ContextTypes.Peek();
        private Scope CurrentScope = default;
        private (bool IsLeftValue, ModulePosition Position) LeftValueChecker;

        public MIRGenerator(CompilationTower tower) : base(tower)
        {
        }

        public MIR Generate()
        {
            WalkDeclarations();

            Tower.CheckDiagnostic();
            return Module.Build();
        }

        private static ZapType[] GetParameterTypes(ParameterListNode parameters)
        {
            var result = new ZapType[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                result[i] = parameters.Parameters[i].Type;

            return result;
        }

        private void GenerateFunctionBlock(BlockNode block)
        {
            for (int i = 0; i < block.Statements.Count; i++)
            {
                var statement = block.Statements[i];
                FunctionBuilder.EmitComment($"node: {statement.NodeKind}");
                RecognizeStatement(statement, i == block.Statements.Count - 1);
            }
        }

        private void RecognizeStatement(INode opaque, bool isLastNodeOfBlock)
        {
            switch (opaque)
            {
                case BadNode: break;
                case VariableStatement statement:
                    GenerateVarStatement(statement, statement.IsConst);
                    break;
                case AssignmentStatement statement:
                    GenerateAssignmentStatement(statement);
                    break;
                case ReturnStatement statement:
                    GenerateReturnStatement(statement);
                    break;
                default:
                    if (!isLastNodeOfBlock)
                        Tower.Report(opaque.Position, "Expression evaluable only when is last of a block");
                    
                    EvaluateExpressionInHiddenBuffer(opaque);
                    break;
            }
        }

        private void EvaluateExpressionInHiddenBuffer(INode expression)
        {
            if (CurrentScope.IsInFunctionBlock)
            {
                ContextTypes.Push(CurrentFunction.ReturnType);
                FixAndCheckTypes(CurrentFunction.ReturnType, EvaluateExpression(expression), expression.Position);
                FunctionBuilder.EmitReturn();
            }
            else
            {
                if (CurrentScope.HiddenAllocationBuffer is null)
                    ContextTypesPushAuto();
                else
                    ContextTypes.Push(CurrentScope.HiddenAllocationBuffer.Type);

                var allocation = TryAllocateHiddenBuffer(EvaluateExpression(expression));
                FixAndCheckTypes(allocation.Type, allocation.Type, expression.Position);

                FunctionBuilder.EmitStoreLocal(
                    MIRValue.StaticMemoryAddress(allocation.StackIndex, allocation.Type));
            }
        }

        private AllocationData TryAllocateHiddenBuffer(ZapType type)
        {
            if (CurrentScope.HiddenAllocationBuffer is null)
            {
                FunctionBuilder.DeclareAllocation(type);
                CurrentScope.HiddenAllocationBuffer = new(FunctionBuilder.GetAllocationNumber(), type, false);
            }

            return CurrentScope.HiddenAllocationBuffer;
        }

        private void GenerateReturnStatement(ReturnStatement statement)
        {
            if (statement.IsVoid())
                FixAndCheckTypes(CurrentFunction.ReturnType, ZapType.Void, statement.Position);
            else
            {
                ContextTypes.Push(CurrentFunction.ReturnType);
                FixAndCheckTypes(CurrentFunction.ReturnType, EvaluateExpression(statement.Body), statement.Body.Position);
                ContextTypes.Pop();
            }

            FunctionBuilder.EmitReturn();
        }

        private void GenerateAssignmentStatement(AssignmentStatement statement)
        {
            ContextTypesPushAuto();
            SetLeftValueChecker(statement.Position);

            var variable = EvaluateExpression(statement.Name);

            CleanLeftValueChecker();
            ContextTypes.Pop();

            var instruction = FunctionBuilder.PopLastInstruction();

            if (instruction.ParameterValue.Kind != MIRValueKind.StaticMemoryAddress)
                Tower.Report(statement.Name.Position, $"Expression in left side of assignment");

            ContextTypes.Push(variable);

            var expressiontype = EvaluateExpression(statement.Body);

            FixAndCheckTypes(variable, expressiontype, statement.Body.Position);

            ContextTypes.Pop();

            instruction.Kind = GetLeftExpressionInstruction(instruction.Kind);
            FunctionBuilder.EmitInstruction(instruction);
        }

        private void CleanLeftValueChecker()
        {
            LeftValueChecker.IsLeftValue = false;
        }

        private void SetLeftValueChecker(ModulePosition position)
        {
            LeftValueChecker.IsLeftValue = true;
            LeftValueChecker.Position = position;
        }

        private void ContextTypesPushAuto()
        {
            ContextTypes.Push(ZapType.Solved(SolvedType.Primitive(TypeKind.Auto)));
        }

        private static MIRValueKind GetLeftExpressionInstruction(MIRValueKind kind)
        {
            return kind + 1;
        }

        private void GenerateVarStatement(VariableStatement statement, bool isconst)
        {
            ContextTypes.Push(statement.Type);

            if (!statement.IsAssigned)
            {
                if (isconst)
                    Tower.Report(statement.Position, "A constant declaration requires a body");

                if (statement.Type.SolvedType.Kind == TypeKind.Auto)
                    Tower.Report(statement.Position, "Type notation needed");
            }

            var expressiontype = EvaluateExpression(!statement.IsAssigned ? GetDefaultValueOf(statement.Type) : statement.Body);

            FixAndCheckTypes(statement.Type, expressiontype, statement.Body.Position);
            var allocation = DeclareVirtualMemorySymbol(statement.Name, statement.Type, statement.Position, isconst);

            FunctionBuilder.EmitStoreLocal(allocation);

            ContextTypes.Pop();
        }

        private static INode GetDefaultValueOf(ZapType type)
        {
            return new BadNode();
        }

        private void FixAndCheckTypes(ZapType expected, ZapType gottype, ModulePosition position)
        {
            FixAuto(expected, gottype);

            var rightsolved = expected.SolvedType;
            var leftsolved = gottype.SolvedType;

            if (rightsolved.Kind != leftsolved.Kind || rightsolved.Base is not null && !rightsolved.Base.Equals(leftsolved.Base))
                Tower.Report(position, $"Type mismatch: expected type '{expected}', but got '{gottype}'");
        }

        private static void FixAuto(ZapType type, ZapType expressiontype)
        {
            if (type.SolvedType.Kind == TypeKind.Auto)
                type.Solve(expressiontype.SolvedType);
        }

        private ZapType EvaluateExpression(INode body)
        {
            var type = ContextType;

            switch (body)
            {
                case BadNode: break;
                case Token expression:
                    type = expression.Kind == TokenKind.Identifier ?
                        EvaluateIdentifier(expression.Value, expression.Position) :
                        EvaluateConstant(expression);
                    break;
                case TypeAllocationNode expression:
                    type = EvaluateTypeAllocationNode(expression);
                    break;
                case MemberNode expression:
                    type = EvaluateMemberNode(expression);
                    break;
                case BinaryExpressionNode expression:
                    type = EvaluateBinaryExpression(expression);
                    break;
                default:
                    CompilationTower.Todo($"implement {body} in MIRGenerator.EvaluateExpression");
                    break;
            }

            return type;
        }

        private ZapType EvaluateBinaryExpression(BinaryExpressionNode expression)
        {
            var leftisconstant = IsConstantInt(expression.Left);
            var rightisconstant = IsConstantInt(expression.Right);
            ZapType type;

            // make it better
            if (leftisconstant & rightisconstant)
            {
                var constant = MIRValue.Constant(
                    type = CoercedOr(ZapType.Int32),
                    ulong.Parse(FoldConstantIntoToken(expression).Value));

                FunctionBuilder.EmitLoadConstantValue(constant);
            }
            else
            {
                if (leftisconstant)
                {
                    var index = FunctionBuilder.CurrentIndex();
                    var left = FoldConstantIntoToken(expression.Left);
                    FunctionBuilder.EmitLoadConstantValue(
                        MIRValue.Constant(
                            type = EvaluateExpression(expression.Right),
                            ulong.Parse(left.Value)));

                    FunctionBuilder.MoveLastInstructionTo(index);
                }
                else if (rightisconstant)
                {
                    var right = FoldConstantIntoToken(expression.Right);
                    FunctionBuilder.EmitLoadConstantValue(
                        MIRValue.Constant(type = EvaluateExpression(expression.Left), ulong.Parse(right.Value)));
                }
                else
                {
                    type = EvaluateExpression(expression.Left);
                    FixAndCheckTypes(type, EvaluateExpression(expression.Right), expression.Position);
                    // allow user defined operators
                }

                EmitOperation(expression.Operator);
            }

            return type;
        }

        private void EmitOperation(TokenKind op)
        {
            FunctionBuilder.EmitInstruction(op switch
            {
                TokenKind.Plus => MIRValueKind.Add,
                TokenKind.Minus => MIRValueKind.Sub,
                TokenKind.Star => MIRValueKind.Mul,
                TokenKind.Slash => MIRValueKind.Div,
                _ => throw new()
            });
        }

        private static Token FoldConstantIntoToken(INode opaque)
        {
            // make it better when introduce floating points
            return opaque switch
            {
                Token expression => expression,
                BinaryExpressionNode expression => new Token(
                    TokenKind.ConstantDigit,
                    FoldConstants(
                        ulong.Parse(FoldConstantIntoToken(expression.Left).Value),
                        ulong.Parse(FoldConstantIntoToken(expression.Right).Value),
                        expression.Operator).ToString(), expression.Position, false),
                _ => Token.NewInfo(TokenKind.Bad, "")
            };
        }

        private static ulong FoldConstants(ulong left, ulong right, TokenKind op)
        {
            return op switch
            {
                TokenKind.Plus => left + right,
                TokenKind.Minus => left - right,
                TokenKind.Star => left * right,
                TokenKind.Slash => left / right,
            };
        }

        private static bool IsConstantInt(INode node)
        {
            return
                (node is Token token && token.Kind == TokenKind.ConstantDigit) ||
                (node is BinaryExpressionNode binary && IsConstantInt(binary.Left) && IsConstantInt(binary.Right));
        }

        private ZapType EvaluateMemberNode(MemberNode expression)
        {
            var basetype = EvaluateExpression(expression.Base);
            if (!basetype.SolvedType.IsNewOperatorAllocable())
            {
                Tower.Report(expression.Base.Position, $"Type '{basetype}' is not accessible via operator '.'");
                return ContextType;
            }

            var structure = basetype.SolvedType.GetStruct();

            var type = GetFieldType(expression.Member.Value, structure.Type.BodyFields, out var index);
            if (type is null)
                Tower.Report(expression.Member.Position, $"Type '{structure.Type.Name}' does not contain a definition for '{expression.Member.Value}'");
            else
                FunctionBuilder.EmitLoadField(MIRValue.StaticMemoryAddress(index, type));

            return type ?? ContextType;
        }

        private ZapType EvaluateTypeAllocationNode(TypeAllocationNode expression)
        {
            var type = expression.Name.SolvedType;

            if (!type.IsNewOperatorAllocable())
            {
                Tower.Report(expression.Position, $"Unable to allocate type '{type}' via operator 'new'");
                return expression.Name;
            }

            var result = type.GetStruct();
            var structure = result.Type;
            var assignedFields = new List<string>();

            FunctionBuilder.EmitLoadZeroinitializedStruct(expression.Name);

            for (int i = 0; i < expression.Body.Count; i++)
            {
                var field = expression.Body[i];

                if (assignedFields.Contains(field.Name))
                {
                    Tower.Report(field.Position, $"Field '{field.Name}' assigned multiple times");
                    continue;
                }

                assignedFields.Add(field.Name);

                var fieldtype = GetFieldType(field.Name, structure.BodyFields, out var fieldindex);
                if (fieldtype is null)
                {
                    Tower.Report(field.Position, $"Type '{structure.Name}' does not contain a definition for '{field.Name}'");
                    continue;
                }

                ContextTypes.Push(fieldtype);

                FunctionBuilder.EmitDupplicate();
                FixAndCheckTypes(fieldtype, EvaluateExpression(field.Body), field.Position);
                FunctionBuilder.EmitStoreField(MIRValue.StaticMemoryAddress(fieldindex, fieldtype));

                ContextTypes.Pop();
            }

            return ZapType.Solved(SolvedType.Struct(result));
        }

        private static ZapType GetFieldType(string name, List<FieldNode> body, out int i)
        {
            for (i = 0; i < body.Count; i++)
            {
                var field = body[i];
                if (name == field.Name)
                    return field.Type;
            }

            return null;
        }

        private ZapType EvaluateIdentifier(string value, ModulePosition position)
        {
            if (!VirtualMemory.TryGetValue(value, out var allocation))
            {
                allocation = new AllocationData(0, ContextType, false);
                Tower.Report(position, $"Variable '{value}' is not declared");
            }
            else
            {
                if (allocation.IsConst && LeftValueChecker.IsLeftValue)
                    Tower.Report(LeftValueChecker.Position, "Constant allocation in left side of assignement");

                FunctionBuilder.EmitLoadLocal(MIRValue.StaticMemoryAddress(allocation.StackIndex, allocation.Type));
            }

            return allocation.Type;
        }

        private ZapType EvaluateConstant(Token expression)
        {
            ZapType result = null;
            MIRValue value;

            switch (expression.Kind)
            {
                case TokenKind.ConstantDigit:
                    result = CoercedOr(ZapType.Solved(SolvedType.Primitive(TypeKind.Int32)));
                    value = MIRValue.Constant(result, ulong.Parse(expression.Value));
                    break;
                default:
                    CompilationTower.Todo($"implement {expression.Kind} in MIRGenerator.EvaluateConstant");
                    value = new();
                    break;
            }

            FunctionBuilder.EmitLoadConstantValue(value);

            return result;
        }

        private ZapType CoercedOr(ZapType or)
        {
            var contexttype = ContextTypes.Peek();
            return contexttype.SolvedType.IsInt() ? contexttype : or;
        }

        private MIRValue DeclareVirtualMemorySymbol(string name, ZapType type, ModulePosition position, bool isconst)
        {
            var localindex = VirtualMemory.Count;
            if (!VirtualMemory.TryAdd(name, new(localindex, type, isconst)))
                Tower.Report(position, $"Variable '{name}' is already declared");

            FunctionBuilder.DeclareAllocation(type);

            return MIRValue.StaticMemoryAddress(localindex, type);
        }

        private void GenerateFunction(FunctionStatement func)
        {
            FunctionBuilder = new MIRFunctionBuilder(func.Name, func.ReturnType, GetParameterTypes(func.ParameterList));
            CurrentFunction = func;
            CurrentScope = new(FunctionBuilder, null, true);
            VirtualMemory = new();

            AllocateParameters(func.ParameterList);
            GenerateFunctionBlock(func.Body);
            FunctionBuilder.EmitOptionalReturnVoid();

            Module.DefineFunction(FunctionBuilder.Build());
        }

        private void AllocateParameters(ParameterListNode parameters)
        {
            foreach (var parameter in parameters.Parameters)
                DeclareVirtualMemorySymbol(parameter.Name, parameter.Type, parameter.Position, false);
        }

        private void CheckRecursiveType(TypeStatement type, List<string> illegaltypes)
        {
            illegaltypes.Add(type.Name);

            foreach (var field in type.BodyFields)
            {
                var fieldtype = field.Type.SolvedType;
                if (fieldtype.IsStruct() ||
                    (fieldtype.IsPointer() && fieldtype.GetBaseElementType().SolvedType.IsStruct()))
                {
                    var fieldstructtype = (fieldtype.Kind == TypeKind.Pointer ? ((ZapType)fieldtype.Base).SolvedType : fieldtype).GetStruct().Type;

                    if (illegaltypes.Contains(fieldtype.ToString()))
                    {
                        Tower.Report(type.Position, "Recursive type");
                        Tower.Report(field.Type.Position, $"Use '?{fieldtype}' instead");
                    }
                    else
                        CheckRecursiveType(fieldstructtype, illegaltypes);
                }
            }
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
                case StructSymbol structsymbol:
                    CheckRecursiveType(structsymbol.Type, new());
                    break;
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
