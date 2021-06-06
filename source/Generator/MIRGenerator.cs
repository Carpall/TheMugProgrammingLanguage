using Mug.Compilation;
using Mug.Generator.IR;
using Mug.Generator.IR.Builder;
using Mug.Lexer;
using Mug.Parser;
using Mug.Parser.AST;
using Mug.Parser.AST.Statements;
using Mug.Parser.ASTLowerer;
using Mug.Symbols;
using Mug.TypeResolution;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mug.Generator
{
    public class MIRGenerator : CompilerComponent
    {
        private const string EntryPointName = "main";

        public MIRModuleBuilder Module { get; } = new();

        private Lowerer Lowerer { get; set; }
        private MIRFunctionBuilder FunctionBuilder { get; set; }
        private FunctionStatement CurrentFunction { get; set; }
        private TypeStatement CurrentType { get; set; }
        private Stack<DataType> ContextTypes { get; set; } = new();
        
        private DataType ContextType => ContextTypes.Peek();

        private Scope CurrentScope = default;
        private (bool IsLeftValue, ModulePosition Position) LeftValueChecker;

        public MIRGenerator(CompilationTower tower) : base(tower)
        {
            Lowerer = new(Tower);
        }

        public MIR Generate()
        {
            SetUpGlobals();
            WalkDeclarations();
            Tower.CheckDiagnostic();

            return Module.Build();
        }

        private void SetUpGlobals()
        {
        }

        private MIRType[] GetParameterTypes(ParameterListNode parameters)
        {
            var result = new MIRType[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
                result[i] = LowerDataType(parameters.Parameters[i].Type);

            return result;
        }

        private MIRType LowerDataType(DataType type)
        {
            var kind = type.SolvedType.Kind;
            return kind switch
            {
                TypeKind.Undefined => new(),

                TypeKind.Int8
                or TypeKind.Int16
                or TypeKind.Int32
                or TypeKind.Int64 => new(MIRTypeKind.Int, GetIntBitSize(kind)),

                TypeKind.UInt8
                or TypeKind.UInt16
                or TypeKind.UInt32
                or TypeKind.UInt64 => new(MIRTypeKind.UInt, GetIntBitSize(kind)),

                TypeKind.Char => new(MIRTypeKind.UInt, 8),
                TypeKind.Bool => new(MIRTypeKind.UInt, 1),
                TypeKind.Void => new(MIRTypeKind.Void),

                TypeKind.DefinedType => new(
                    MIRTypeKind.Struct,
                    LowerStruct(type.SolvedType.GetStruct())),

                _ => ToImplement<MIRType>(kind.ToString(), nameof(LowerDataType))
            };
        }

        private MIRStructure LowerStruct(TypeStatement type)
        {
            if (type.LoweredStructCache.HasValue)
                return type.LoweredStructCache.Value;

            var result = new MIRStructure(type.IsPacked, type.Name, LowerStructBody(type.BodyFields));

            type.CacheLoweredStruct(result);
            Module.DefineStruct(result);
            return result;
        }

        private MIRType[] LowerStructBody(List<FieldNode> bodyFields)
        {
            var result = new MIRType[bodyFields.Count];
            for (int i = 0; i < bodyFields.Count; i++)
                result[i] = LowerDataType(bodyFields[i].Type);

            return result;
        }

        private static int GetIntBitSize(TypeKind kind)
        {
            var value = kind.ToString();
            return value.Last() == '8' ? 8 : int.Parse(value.Substring(value.Length - 2, 2));
        }

        private void GenerateBlock(BlockNode block)
        {
            for (var i = 0; i < block.Statements.Count; i++)
                RecognizeStatement(block.Statements[i], i == block.Statements.Count - 1);
        }

        private void RecognizeStatement(INode opaque, bool isLastNodeOfBlock)
        {
            switch (opaque)
            {
                case BadNode: break;
                case VariableStatement statement:
                    GenerateVarStatement(statement);
                    break;
                case AssignmentStatement statement:
                    GenerateAssignmentStatement(statement);
                    break;
                case ReturnStatement statement:
                    GenerateReturnStatement(statement);
                    break;
                case CallStatement statement:
                    GenerateCallStatement(statement, isLastNodeOfBlock);
                    break;
                case ConditionalStatement statement:
                    GenerateConditionalStatement(statement, isLastNodeOfBlock);
                    break;
                case ForLoopStatement statement:
                    GenerateForLoopStatement(statement);
                    break;
                case PostfixOperator statement:
                    GeneratePostfixOperator(statement);
                    break;
                default:
                    ReportExpressionAsStatementWhenNotLastOfBlock(opaque, isLastNodeOfBlock);
                    EvaluateExpressionAsStatement(opaque);
                    break;
            }
        }

        private void GeneratePostfixOperator(PostfixOperator statement)
        {
            GenerateAssignmentStatement(Lowerer.LowerPostfixStatementToAssignment(statement));
        }

        private void ReportExpressionAsStatementWhenNotLastOfBlock(INode opaque, bool isLastNodeOfBlock)
        {
            if (!isLastNodeOfBlock)
                Tower.Report(opaque.Position, "Expression evaluable only when is last of a block");
        }

        private void GenerateForLoopStatement(ForLoopStatement statement)
        {
            SaveOldScope(out var oldScope);
            ScopeCoveredForLoopStatement(statement);
            RestoreOldScope(oldScope);
        }

        private void ScopeCoveredForLoopStatement(ForLoopStatement statement)
        {
            var conditionBlock = CreateBlock("cond");
            var thenBlock = CreateBlock("body");
            var endBlock = CreateBlock("end");

            EvaluateForLoopConditionAndGetConditionBlock(statement.LeftExpression, statement.ConditionExpression, conditionBlock, thenBlock, endBlock);

            SwitchBlock(thenBlock);
            ContextTypes.Push(DataType.Void);
            GenerateBlock(statement.Body);
            ContextTypes.Pop();

            RecognizeStatement(statement.RightExpression, false);
            FunctionBuilder.EmitJump(conditionBlock.Index);

            SwitchBlock(endBlock);
        }

        private void EvaluateForLoopConditionAndGetConditionBlock(
            INode leftExpression, INode conditionExpression, MIRBlock conditionBlock, MIRBlock thenBlock, MIRBlock endBlock)
        {
            RecognizeStatement(leftExpression, false);

            FunctionBuilder.EmitJump(conditionBlock.Index);

            SwitchBlock(conditionBlock);
            EvaluateConditionInConditionalStatement(conditionExpression, thenBlock, endBlock);
        }

        private void EvaluateConditionInConditionalStatement(INode expression, MIRBlock then, MIRBlock endBlock)
        {
            ContextTypes.Push(DataType.Bool);
            var condition = EvaluateExpression(expression);
            ContextTypes.Pop();

            FixAndCheckTypes(DataType.Bool, condition, expression.Position);

            FunctionBuilder.EmitJumpCondition(then.Index, endBlock.Index);
        }

        private void EvaluateExpressionAsStatement(INode expression)
        {
            var type = EvaluateExpression(expression);
            FixAndCheckTypes(ContextType, type, expression.Position);

            ManageScopeType(expression.Position, true, type);
        }

        private void GenerateConditionalStatement(ConditionalStatement statement, bool isLastOfBlock)
        {
            if (statement.Kind is TokenKind.KeyWhile)
                GenerateWhileLoopStatement(statement);
            else
                GenerateIFStatement(statement, isLastOfBlock);
        }

        private void GenerateWhileLoopStatement(ConditionalStatement statement)
        {
            var conditionBlock = CreateBlock("cond");
            var then = CreateBlock("body");
            var endBlock = CreateBlock("end");

            FunctionBuilder.EmitJump(conditionBlock.Index);

            SwitchBlock(conditionBlock);
            EvaluateConditionInConditionalStatement(statement.Expression, then, endBlock);

            SwitchBlock(then);
            ContextTypes.Push(DataType.Void);
            GenerateNodeBlockInExpression(statement.Body);
            ContextTypes.Pop();

            FunctionBuilder.EmitJump(conditionBlock.Index);

            SwitchBlock(endBlock);
        }

        private void GenerateIFStatement(ConditionalStatement statement, bool isLastOfBlock)
        {
            var type = EvaluateConditionalStatement(statement, isLastOfBlock && !ContextType.SolvedType.IsVoid());
            ManageScopeType(statement.Position, isLastOfBlock, type);
        }

        private DataType EvaluateConditionalStatement(ConditionalStatement node, bool isExpression, MIRBlock endBlock = null)
        {
            var isFirst = endBlock is null;

            var thenBlock = CreateBlock("then");
            endBlock ??= CreateBlock("end");
            var elseBlock = node.ElseNode is not null ? CreateBlock("else") : endBlock;

            if (isFirst)
            {
                if (node.ElseNode is not null)
                    FunctionBuilder.SwapTwoPreviousBlocks();

                if (!isExpression)
                    ContextTypes.Push(DataType.Void);

                ReportMissingElseNode(node, isExpression);
            }

            EvaluateConditionInConditionalStatement(node.Expression, thenBlock, elseBlock);

            SwitchBlock(thenBlock);
            var type = GenerateNodeBlockInExpression(node.Body);
            FunctionBuilder.EmitJump(endBlock.Index);

            if (elseBlock != endBlock)
            {
                SwitchBlock(elseBlock);
                FixAndCheckTypes(type, EvaluateElseNode(node.ElseNode, endBlock), node.ElseNode.Position);
            }

            SwitchBlock(endBlock);

            if (isFirst && !isExpression)
                ContextTypes.Pop();

            return type;
        }

        private DataType EvaluateElseNode(ConditionalStatement elseNode, MIRBlock endBlock)
        {
            if (elseNode.Kind is TokenKind.KeyElif)
                return EvaluateConditionalStatement(elseNode, false, endBlock);

            var type = GenerateNodeBlockInExpression(elseNode.Body);
            FunctionBuilder.EmitJump(endBlock.Index);
            return type;
        }

        private void SwitchBlock(MIRBlock block)
        {
            FunctionBuilder.SwitchBlock(block);
        }

        private MIRBlock CreateBlock(string identifier)
        {
            var index = FunctionBuilder.CurrentBlockIndex();
            var block = new MIRBlock(index, identifier);

            FunctionBuilder.AddBlock(block);

            return block;
        }

        private void ReportMissingElseNode(ConditionalStatement expression, bool isExpression)
        {
            if (isExpression && !HasElseBody(expression))
                Tower.Report(expression.Position, "Condition in expression must have a 'else' node");
        }

        private DataType GetHiddenBufferTypeOrVoid()
        {
            return CurrentScope.Type ?? DataType.Void;
        }

        private static bool HasElseBody(ConditionalStatement condition)
        {
            while (condition is not null)
            {
                if (condition.Expression is BadNode)
                    return true;

                condition = condition.ElseNode;
            }

            return false;
        }

        private void GenerateCallStatement(CallStatement statement, bool isLastOfBlock)
        {
            var type = EvaluateNodeCall(statement, !isLastOfBlock | ContextType.SolvedType.IsVoid());
            ManageScopeType(statement.Position, isLastOfBlock, type);
        }

        private void ManageScopeType(ModulePosition position, bool isLastOfBlock, DataType type)
        {
            if (isLastOfBlock & !ContextType.SolvedType.IsVoid())
                ManageScopeTypeNotVoid(type, position);
            else if (!type.SolvedType.IsVoid())
                FunctionBuilder.EmitPop();
        }

        private void ManageScopeTypeNotVoid(DataType expressionType, ModulePosition position)
        {
            if (CurrentScope.Type is null)
                CurrentScope.Type = expressionType;
            else
            {
                FixAndCheckTypes(CurrentScope.Type, expressionType, position);
                EmitReturnIfExpressionAsStatementIsInFunctionBlock(expressionType);
            }
        }

        private void EmitReturnIfExpressionAsStatementIsInFunctionBlock(DataType type)
        {
            if (CurrentScope.IsInFunctionBlock)
                FunctionBuilder.EmitReturn(LowerDataType(type));
        }

        private void GenerateReturnStatement(ReturnStatement statement)
        {
            if (statement.IsVoid())
                FixAndCheckTypes(CurrentFunction.ReturnType, DataType.Void, statement.Position);
            else
            {
                ContextTypes.Push(CurrentFunction.ReturnType);
                FixAndCheckTypes(CurrentFunction.ReturnType, EvaluateExpression(statement.Body), statement.Body.Position);
                ContextTypes.Pop();
            }

            FunctionBuilder.EmitReturn(LowerDataType(CurrentFunction.ReturnType));
        }

        private void GenerateAssignmentStatement(AssignmentStatement statement)
        {
            Lowerer.LowerAssignmentStatementOperator(ref statement);

            ContextTypesPushUndefined();
            SetLeftValueChecker(statement.Position);

            var variable = EvaluateExpression(statement.Name);

            if (variable.SolvedType.IsUndefined())
                return;

            var instruction = EvaluateLeftSideOfAssignment(statement, variable);

            var expressiontype = EvaluateExpression(statement.Body);

            FixAndCheckTypes(variable, expressiontype, statement.Body.Position);

            ContextTypes.Pop();

            instruction.Kind = GetLeftExpressionInstruction(instruction.Kind);
            FunctionBuilder.EmitInstruction(instruction);
        }

        private MIRInstruction EvaluateLeftSideOfAssignment(AssignmentStatement statement, DataType variable)
        {
            CleanLeftValueChecker();
            ContextTypes.Pop();

            var instruction = FunctionBuilder.PopLastInstruction();

            if (!IsConvertibleToLeftExpressionInstruction(instruction.Kind))
                Tower.Report(statement.Name.Position, $"Expression in left side of assignment");

            ContextTypes.Push(variable);
            return instruction;
        }

        private static bool IsConvertibleToLeftExpressionInstruction(MIRInstructionKind kind)
        {
            return kind is MIRInstructionKind.LoadLocal or MIRInstructionKind.LoadField;
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

        private void ContextTypesPushUndefined()
        {
            ContextTypes.Push(DataType.Primitive(TypeKind.Undefined));
        }

        private static MIRInstructionKind GetLeftExpressionInstruction(MIRInstructionKind kind)
        {
            return kind + 1;
        }

        private void GenerateVarStatement(VariableStatement statement)
        {
            CheckVariable(statement);

            ContextTypes.Push(statement.Type);

            var expressiontype = EvaluateExpression(!statement.IsAssigned ? GetDefaultValueOf(statement.Type) : statement.Body);

            FixAndCheckTypes(statement.Type, expressiontype, statement.Body.Position);
            var allocation = DeclareVirtualMemorySymbol(statement.Name, statement.Type, statement.Position, statement.IsConst);

            FunctionBuilder.EmitStoreLocal(allocation.StackIndex, LowerDataType(allocation.Type));

            ContextTypes.Pop();
        }

        private void CheckVariable(VariableStatement statement)
        {
            if (statement.IsAssigned) return;
            
            if (statement.IsConst)
                Tower.Report(statement.Position, "A constant declaration requires a body");
            if (statement.Type.SolvedType.Kind is TypeKind.Auto)
                Tower.Report(statement.Position, "Type notation needed");
        }

        private static INode GetDefaultValueOf(DataType type)
        {
            CompilationTower.Todo($"implement {nameof(GetDefaultValueOf)}");
            return new BadNode();
        }

        private void FixAndCheckTypes(DataType expected, DataType gottype, ModulePosition position)
        {
            FixAuto(expected, gottype);
            if (AreNotCompatible(expected, gottype))
                Tower.Report(position, $"Type mismatch: expected type '{expected}', but got '{gottype}'");
        }

        private static bool AreNotCompatible(DataType expected, DataType gottype)
        {
            var rightsolved = expected.SolvedType;
            var leftsolved = gottype.SolvedType;

            return
                rightsolved.Kind != leftsolved.Kind
                || rightsolved.Base is not null
                && !rightsolved.Base.Equals(leftsolved.Base);
        }

        private static void FixAuto(DataType type, DataType expressiontype)
        {
            if (type.SolvedType.Kind == TypeKind.Auto)
                type.Solve(expressiontype.SolvedType);
        }

        private DataType EvaluateExpression(INode body)
        {
            var type = body switch
            {
                BadNode => ContextType,
                Token expression => EvaluateToken(expression),
                BooleanBinaryExpressionNode expression => EvaluateBooleanBinaryExpression(expression),
                TypeAllocationNode expression => EvaluateNodeTypeAllocation(expression),
                MemberNode expression => EvaluateMemberNode(expression),
                BinaryExpressionNode expression => EvaluateNodeBinaryExpression(expression),
                BlockNode expression => GenerateNodeBlockInExpression(expression),
                CallStatement expression => EvaluateNodeCall(expression, false),
                ConditionalStatement expression => EvaluateConditionalStatement(expression, true),
                PrefixOperator expression => EvaluatePrefixOperator(expression),
                _ => ToImplement<DataType>(body.ToString(), nameof(EvaluateExpression)),
            };

            if (type.SolvedType.IsVoid())
                Tower.Report(body.Position, "Expected a non-void expression");

            return type;
        }

        private DataType EvaluatePrefixOperator(PrefixOperator expression)
        {
            var expr =
                !IsConstant(expression.Expression) ?
                    expression.Expression :
                    FoldConstantIntoToken(expression.Expression);

            var type = EvaluateExpression(expr);
            var result = type;

            switch (expression.Prefix.Kind)
            {
                case TokenKind.Plus:
                    ExpectIntTypeForPlusPrefixOperator(type, expression.Prefix.Position);
                    break;
                case TokenKind.Minus:
                    EvaluateMinusPrefixOperator(expression, type);
                    break;
                case TokenKind.Negation:
                    EvaluateNegationPrefixOperator(expression, type, ref result);
                    break;
                /*case TokenKind.Star:
                    break;
                case TokenKind.BooleanAND:
                    break;*/
                default:
                    ToImplement<object>(expression.Prefix.ToString(), nameof(EvaluatePrefixOperator));
                    break;
            }

            return result;
        }

        private void ExpectIntTypeForPlusPrefixOperator(DataType type, ModulePosition position)
        {
            if (type.SolvedType.IsInt())
                Tower.Report(position, $"Unable to apply operator '+' over type '{type}'");
        }

        private void EvaluateMinusPrefixOperator(PrefixOperator expression, DataType type)
        {
            ExpectSignedIntForMinusPrefixOperator(type, expression.Prefix.Position);
            FunctionBuilder.EmitNeg(LowerDataType(type));
        }

        private void EvaluateConstantMinusPrefixOperator(PrefixOperator expression, DataType type, ref string resultValue)
        {
            ExpectSignedIntForMinusPrefixOperator(type, expression.Prefix.Position);
            if (long.TryParse(resultValue, out var value))
                resultValue = (-value).ToString();
        }

        private void EvaluateNegationPrefixOperator(PrefixOperator expression, DataType type, ref DataType result)
        {
            FixAndCheckTypes(DataType.Bool, type, expression.Prefix.Position);
            FunctionBuilder.EmitNeg(LowerDataType(DataType.Bool));

            result = DataType.Bool;
        }

        private void ExpectSignedIntForMinusPrefixOperator(DataType type, ModulePosition position)
        {
            if (!type.SolvedType.IsSignedInt())
                Tower.Report(position, $"Unable to apply operator '-' over type '{type}'");
        }

        private DataType EvaluateToken(Token expression)
        {
            return
                expression.Kind == TokenKind.Identifier ?
                    EvaluateIdentifier(expression.Value, expression.Position) :
                    EvaluateConstant(expression);
        }

        private DataType EvaluateBooleanBinaryExpression(BooleanBinaryExpressionNode expression)
        {
            var leftIsConstant = IsConstant(expression.Left);
            var rightIsConstant = IsConstant(expression.Right);

            if (leftIsConstant & rightIsConstant)
                EvaluateLeftAndRightConstantInBooleanExpression(expression);
            else
            {
                var type = EvaluateSemiConstantBooleanBinaryOrNonConstant(
                    expression,
                    IsConstantInt(expression.Left),
                    IsConstantInt(expression.Right));

                EmitOperation(expression.Operator.Kind, type);
            }

            return DataType.Bool;
        }

        private void CheckUnsignedOverflow(long constant, ModulePosition position)
        {
            if (ContextType.SolvedType.IsUnsignedInt() && IsNegative(constant))
                Tower.Report(position, "Unsigned constant operation overflows");
        }

        private static bool IsNegative(long constant)
        {
            return constant < 0;
        }

        private DataType EvaluateSemiConstantBooleanBinaryOrNonConstant(
            BooleanBinaryExpressionNode expression,
            bool leftIsConstant,
            bool rightIsConstant)
        {
            return
                leftIsConstant ?
                    EvaluateLeftBooleanBinaryIsConstant(expression) :
                    rightIsConstant ?
                        EvaluateRightIsBooleanBinaryConstant(expression) :
                        EvaluateBooleanBinaryNoConstants(expression);
        }

        private DataType EvaluateBooleanBinaryNoConstants(BooleanBinaryExpressionNode expression)
        {
            var leftType = EvaluateTermOfBooleanBinaryOmittingConstantBoolean(expression.Left);
            var rightType = EvaluateTermOfBooleanBinaryOmittingConstantBoolean(expression.Right);

            CheckOperatorImplementation(leftType, expression.Operator.Kind, expression.Operator.Position);
            CheckLeftAndRightTypes(leftType, rightType, expression.Position);

            return leftType;
        }

        private DataType EvaluateTermOfBooleanBinaryOmittingConstantBoolean(INode term)
        {
            var result = EvaluateExpression(term);

            if (term is Token { Kind: TokenKind.ConstantBoolean })
                Tower.Warn(term.Position, "Constant boolean in boolean expression");

            return result;
        }

        private DataType EvaluateRightIsBooleanBinaryConstant(BooleanBinaryExpressionNode expression)
        {
            var right = FoldConstantIntoToken(expression.Right);

            var leftType = EvaluateExpression(expression.Left);
            var constantRight = long.Parse(right.Value);

            ExpectIntTypeLeftTermOfSemiConstantExpression(leftType, expression.Position);

            FunctionBuilder.EmitLoadConstantValue(constantRight, LowerDataType(leftType));

            return leftType;
        }

        private DataType EvaluateLeftBooleanBinaryIsConstant(BooleanBinaryExpressionNode expression)
        {
            var index = FunctionBuilder.CurrentIndex();
            var left = FoldConstantIntoToken(expression.Left);

            ContextTypesPushUndefined();

            var rightType = EvaluateExpression(expression.Right);

            ExpectIntTypeRightTermOfSemiConstantExpression(rightType, expression.Position);
            FunctionBuilder.EmitLoadConstantValue(long.Parse(left.Value), LowerDataType(rightType));

            ContextTypes.Pop();

            FunctionBuilder.MoveLastInstructionTo(index);
            return rightType;
        }

        private void EvaluateLeftAndRightConstantInBooleanExpression(BooleanBinaryExpressionNode expression)
        {
            var result = FoldConstantBooleanExpressionIntoBool(
                expression.Left,
                expression.Right,
                expression.Operator.Kind,
                expression.Position);

            FunctionBuilder.EmitLoadConstantValue(result, LowerDataType(DataType.Bool));
        }

        private bool FoldConstantBooleanExpressionIntoBool(INode left, INode right, TokenKind op, ModulePosition position)
        {
            var foldedLeft = FoldConstantIntoToken(left);
            var foldedRight = FoldConstantIntoToken(right);

            ReportIfTypesAreIncompatible(foldedLeft, foldedRight, position);

            return PerformBooleanConstantOperation(foldedLeft, foldedRight, op);
        }

        private void ReportIfTypesAreIncompatible(Token left, Token right, ModulePosition position)
        {
            if (left.Kind != right.Kind)
                ReportIncompatibleTypes(left.Kind.GetDescription(), right.Kind.GetDescription(), position);
        }

        private static bool PerformBooleanConstantOperation(Token left, Token right, TokenKind op)
        {
            return op switch
            {
                TokenKind.BooleanNEQ => left.Value != right.Value,
                TokenKind.BooleanEQ => left.Value == right.Value,

                TokenKind.BooleanGreater =>
                    IsConstantNumber(left, right, out var constantLeft, out var constantRight)
                    && constantLeft > constantRight,
                TokenKind.BooleanLess =>
                    IsConstantNumber(left, right, out var constantLeft, out var constantRight)
                    && constantLeft < constantRight,

                TokenKind.BooleanGEQ =>
                    IsConstantNumber(left, right, out var constantLeft, out var constantRight)
                    && constantLeft >= constantRight,
                TokenKind.BooleanLEQ =>
                    IsConstantNumber(left, right, out var constantLeft, out var constantRight)
                    && constantLeft <= constantRight,

                _ => false
            };
        }

        private static bool IsConstantNumber(Token left, Token right, out decimal constantLeft, out decimal constantRight)
        {
            _ = decimal.TryParse(left.Value, out constantLeft);
            _ = decimal.TryParse(right.Value, out constantRight);

            return left.Kind is TokenKind.ConstantDigit or TokenKind.ConstantFloatDigit;
        }

        private static bool IsConstant(INode node)
        {
            return node switch
            {
                Token expression => IsTokenConstant(expression),
                BooleanBinaryExpressionNode expression => IsConstantBinary(expression.Left, expression.Right),
                PrefixOperator expression => IsConstant(expression.Expression),
                _ => false
            };
        }

        private static bool IsConstantBinary(INode left, INode right)
        {
            return IsConstant(left) && IsConstant(right);
        }

        private static bool IsTokenConstant(Token value)
        {
            return
                value.Kind is TokenKind.ConstantBoolean
                or TokenKind.ConstantChar
                or TokenKind.ConstantDigit
                or TokenKind.ConstantFloatDigit
                or TokenKind.ConstantString;
        }

        private static T ToImplement<T>(string value, string function)
        {
            CompilationTower.Todo($"implement {value} in {function}");
            return default;
        }

        private DataType EvaluateNodeCall(CallStatement expression, bool isStatement)
        {
            if (expression.IsBuiltIn)
                return EvaluateNodeCallBuiltIn(expression, isStatement);

            var parameters = expression.Parameters;
            var func = EvaluateFunctionName(expression.Name, ref parameters);
            expression.Parameters = parameters;

            if (func is null)
                return ContextType;

            EvaluateCallParameters(expression, func);

            return func.ReturnType;
        }

        private DataType EvaluateNodeCallBuiltIn(CallStatement expression, bool isStatement)
        {
            if (expression.Name is not Token name)
            {
                Tower.Report(expression.Position, "Unsupported composed name");
                return ContextType;
            }

            var type = ContextType;

            switch (name.Value)
            {
                case "size":
                    WarnWhenStatement();
                    type = EvaluateCompTimeSize(expression);
                    break;
                case "u8" or "u16" or "u32" or "u64"
                or "i8" or "i16" or "i32" or "i64":
                    WarnWhenStatement();
                    type = EvaluateCompTimeIntCast(expression, name.Value);
                    break;
                case "exit":
                    type = EvaluateCompTimeExit(expression);
                    break;
                default:
                    Tower.Report(expression.Position, "Unknown builtin function");
                    break;
            }

            return type;

            void WarnWhenStatement()
            {
                if (isStatement)
                    Tower.Warn(expression.Position, "Useless call to builtin function here");
            }
        }

        private DataType EvaluateCompTimeExit(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 0, 1);
            var value = expression.Parameters.FirstOrDefault() ?? Token.NewInfo(TokenKind.ConstantDigit, "0");
            var type = EvaluateExpression(value);
            var result = ContextType;

            FixAuto(result, DataType.Int32);

            var loweredReturnType = LowerDataType(result);
            var parameterType = new MIRType(MIRTypeKind.Int, 32);

            TryDeclareExternPrototype("exit", loweredReturnType, parameterType);

            if (!type.SolvedType.IsInt())
                ReportExpectedValueOfTypeInt(value.Position);
            else if (type.SolvedType.Kind is not TypeKind.Int32)
                FunctionBuilder.EmitCastIntToInt(parameterType);

            FunctionBuilder.EmitCall("exit", loweredReturnType);

            return result;
        }

        private void TryDeclareExternPrototype(string name, MIRType type, params MIRType[] parameterTypes)
        {
            if (!Module.FunctionPrototypeIsDeclared(name))
                Module.DefineFunctionPrototype(name, type, parameterTypes);
        }

        private void ReportExpectedValueOfTypeInt(ModulePosition position)
        {
            Tower.Report(position, "Expected a value of type 'int'");
        }

        private DataType EvaluateCompTimeIntCast(CallStatement expression, string name)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 1);
            var value = expression.Parameters.FirstOrDefault();

            if (value is null)
                return ContextType;

            var type = EvaluateExpression(value);

            // or enum in the future
            if (!type.SolvedType.IsInt())
                ReportExpectedValueOfTypeInt(value.Position);

            type = IntTypeStringToMIRType(name);
            FunctionBuilder.EmitCastIntToInt(LowerDataType(type));
            return type;
        }

        private static DataType IntTypeStringToMIRType(string name)
        {
            var bitSize = int.Parse(name[1..]);
            var isUnsigned = Convert.ToByte(name.First() is 'u');
            return DataType.Primitive((TypeKind)(bitSize / 2 + isUnsigned));
        }

        private DataType EvaluateCompTimeSize(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 1);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 0);
            var type = expression.Generics.FirstOrDefault();
            var returnType = CoercedOr(DataType.Int32);

            if (type is not null)
                FunctionBuilder.EmitLoadConstantValue(GetTypeByteSize(type, type.Position), LowerDataType(returnType));

            return returnType;
        }

        private void ExpectParametersNumber(NodeBuilder parameters, ModulePosition position, params int[] expectedNumbers)
        {
            if (!expectedNumbers.Contains(parameters.Count))
                ReportIncorrectNumberOfElements(parameters.Count, position, expectedNumbers);
        }

        private void ReportIncorrectNumberOfElements(int count, ModulePosition position, int[] expectedNumbers)
        {
            Tower.Report(position, $"Expected '{string.Join("', '", expectedNumbers)}' parameters, but got '{count}'");
        }

        private void ExpectGenericsNumber(List<DataType> generics, ModulePosition position, params int[] expectedNumbers)
        {
            if (!expectedNumbers.Contains(generics.Count))
                ReportIncorrectNumberOfElements(generics.Count, position, expectedNumbers);
        }

        private FunctionStatement EvaluateFunctionName(INode functionName, ref NodeBuilder parameters)
        {
            var funcSymbol = functionName switch
            {
                Token name => CheckAndSearchForFunctionOrStaticMethod(name),
                MemberNode name => EvaluateBaseFunctionName(name, ref parameters),
                _ => FunctionBaseNameToImplement(functionName),
            };

            return funcSymbol;
        }

        private FunctionStatement FunctionBaseNameToImplement(INode functionName)
        {
            Tower.Report(functionName.Position, "Invalid construction");
            throw new();
        }

        private FunctionStatement EvaluateBaseFunctionName(MemberNode name, ref NodeBuilder parameters)
        {
            return
                name.Base is Token baseToken ?
                    EvaluateTokenBaseFunctionName(name, parameters, baseToken) :
                    EvaluateExpressionBaseFunctionName(name, parameters);
        }

        private FunctionStatement EvaluateExpressionBaseFunctionName(MemberNode name, NodeBuilder parameters)
        {
            parameters.Prepend(name.Base);

            var type = GetExpressionType(name.Base);

            if (!type.SolvedType.IsStruct())
                return ReportPrimitiveCannotHaveMethods(name.Base.Position);

            return SearchForMethod(name.Member.Value, type.SolvedType.GetStruct(), name.Member.Position);
        }

        private DataType GetExpressionType(INode expression)
        {
            var oldFunctionBuilder = new MIRFunctionBuilder(FunctionBuilder);

            ContextTypesPushUndefined();
            var type = EvaluateExpression(expression);
            ContextTypes.Pop();

            FunctionBuilder = oldFunctionBuilder;
            return type;
        }

        private FunctionStatement EvaluateTokenBaseFunctionName(MemberNode name, NodeBuilder parameters, Token baseToken)
        {
            if (baseToken.Kind is not TokenKind.Identifier)
                return ReportPrimitiveCannotHaveMethods(baseToken.Position);

            string typeName;
            if (GetLocalVariable(baseToken.Value, out var allocation))
            {
                if (!allocation.Type.SolvedType.IsStruct())
                    return ReportPrimitiveCannotHaveMethods(baseToken.Position);

                parameters.Prepend(baseToken);
                typeName = allocation.Type.SolvedType.GetStruct().Name;
            }
            else
                typeName = baseToken.Value;

            var type = GetType(typeName, baseToken.Position);

            return type is null ? null : SearchForMethod(name.Member.Value, type, name.Member.Position);
        }

        private TypeStatement GetType(string typeName, ModulePosition position)
        {
            return Tower.Symbols.GetSymbol<TypeStatement>(typeName, position, "type");
        }

        private FunctionStatement ReportPrimitiveCannotHaveMethods(ModulePosition position)
        {
            Tower.Report(position, "Primitive types don't support methods");
            return null;
        }

        private FunctionStatement SearchForMethod(string value, TypeStatement type, ModulePosition position)
        {
            foreach (var method in type.BodyMethods)
                if (value == method.Name)
                {
                    ExpectPublicMethodOrInternal(method, type, position);
                    return method;
                }

            Tower.Report(type.Position, $"No method '{value}' declared in type '{type.Name}'");
            return null;
        }

        private void ExpectPublicMethodOrInternal(FunctionStatement method, TypeStatement type, ModulePosition position)
        {
            if (method.Modifier != TokenKind.KeyPub && !ProcessingMethodOf(type))
                Tower.Report(position, $"Method '{method.Name}' is a private member of type '{type.Name}'");
        }

        private void EvaluateCallParameters(CallStatement expression, FunctionStatement func)
        {
            ReportFewParameters(expression, func);

            for (var i = 0; i < expression.Parameters.Count; i++)
                CheckAndEvaluateParameter(func, i, expression.Parameters[i]);

            FunctionBuilder.EmitCall(func.Name, LowerDataType(func.ReturnType));
        }

        private void CheckAndEvaluateParameter(FunctionStatement func, int i, INode parameter)
        {
            if (func.ParameterList.Length <= i)
                ReportExtraParameter(parameter);
            else
                EvaluateExpressionParameter(func, i, parameter);
        }

        private void EvaluateExpressionParameter(FunctionStatement func, int i, INode parameter)
        {
            var prototypeParameterType = func.ParameterList.Parameters[i].Type;
            var expressionType = EvaluateParameter(prototypeParameterType, parameter);
            FixAndCheckTypes(prototypeParameterType, expressionType, parameter.Position);
        }

        private void ReportExtraParameter(INode parameter)
        {
            Tower.Report(parameter.Position, "Unexpected extra function parameter");
            EvaluateParameter(DataType.Auto, parameter);
        }

        private void ReportFewParameters(CallStatement expression, FunctionStatement func)
        {
            if (func.ParameterList.Length > expression.Parameters.Count)
                Tower.Report(
                    expression.Parameters.Position,
                    $"Expected '{func.ParameterList.Length}' function parameter{GetPlural(func.ParameterList.Length)}");
        }

        private static char GetPlural(int count)
        {
            // when 0 the s is required
            return count != 1 ? 's' : '\0';
        }

        private DataType EvaluateParameter(DataType prototypeParameterType, INode passedParameter)
        {
            ContextTypes.Push(prototypeParameterType);

            var type = EvaluateExpression(passedParameter);

            ContextTypes.Pop();

            return type;
        }

        private FunctionStatement CheckAndSearchForFunctionOrStaticMethod(Token name)
        {
            if (name.Kind is not TokenKind.Identifier)
            {
                Tower.Report(name.Position, $"Function cannot named with non-identifier tokens");
                return new() { Name = name.Value, ReturnType = DataType.Int32 };
            }

            if (ProcessingMethod() && IsInternalMethod(name.Value, out var method))
                return method;

            return Tower.Symbols.GetSymbol<FunctionStatement>(name.Value, name.Position, "function");
        }

        private bool IsInternalMethod(string value, out FunctionStatement method)
        {
            method = CurrentType.BodyMethods.Find(m => m.Name == value);
            return method is not null;
        }

        private bool ProcessingMethod()
        {
            return CurrentType is not null;
        }

        private void SaveOldScope(out Scope oldScope)
        {
            oldScope = CurrentScope;
            CurrentScope = new(null, false, CurrentScope.VirtualMemory);
        }

        private void RestoreOldScope(Scope oldScope)
        {
            CurrentScope = oldScope;
        }

        private DataType GenerateNodeBlockInExpression(BlockNode expression)
        {
            var oldScope = CurrentScope;
            CurrentScope = new(null, false, CurrentScope.VirtualMemory);

            GenerateBlock(expression);

            var scopeType = GetHiddenBufferTypeOrVoid();
            CurrentScope = oldScope;
            return scopeType;
        }

        private DataType EvaluateNodeBinaryExpression(BinaryExpressionNode expression)
        {
            var leftIsConstant = IsConstant(expression.Left);
            var rightIsConstant = IsConstant(expression.Right);
            DataType type;

            // make it better
            if (leftIsConstant & rightIsConstant)
                type = EvaluateBinaryConstant(expression);
            else
            {
                type = EvaluateSemiConstantBinaryOrNonConstant(
                    expression,
                    IsConstantInt(expression.Left),
                    IsConstantInt(expression.Right));

                EmitOperation(expression.Operator.Kind, type);
            }

            return type;
        }

        private DataType EvaluateSemiConstantBinaryOrNonConstant(
            BinaryExpressionNode expression,
            bool leftIsConstant,
            bool rightIsConstant)
        {
            return
                leftIsConstant ?
                    EvaluateLeftBinaryIsConstant(expression) :
                    rightIsConstant ?
                        EvaluateRightIsBinaryConstant(expression) :
                        EvaluateBinaryNoConstants(expression);
        }

        private DataType EvaluateBinaryConstant(BinaryExpressionNode expression)
        {
            var constant = long.Parse(FoldConstantIntoToken(expression).Value);

            var type = CoercedOr(DataType.Int32);
            FunctionBuilder.EmitLoadConstantValue(constant, LowerDataType(type));
            CheckUnsignedOverflow(constant, expression.Position);

            return type;
        }

        private DataType EvaluateBinaryNoConstants(BinaryExpressionNode expression)
        {
            var leftType = EvaluateExpression(expression.Left);
            var rightType = EvaluateExpression(expression.Right);

            CheckLeftAndRightTypes(leftType, rightType, expression.Position);
            CheckOperatorImplementation(leftType, expression.Operator.Kind, expression.Operator.Position);

            return leftType;
        }

        private void CheckOperatorImplementation(DataType type, TokenKind op, ModulePosition position)
        {
            if (op is TokenKind.BooleanEQ or TokenKind.BooleanNEQ
                || type.SolvedType.IsInt()
                || type.SolvedType.IsFloat())
                return;

            var supportsOperators = DataType.TypesOperatorsImplementation.TryGetValue(type.SolvedType.Kind, out var operators);

            if (!supportsOperators || !operators.Contains(op))
                Tower.Report(position, $"Type '{type}' does not support operator '{op.GetDescription()}'");
        }

        private DataType EvaluateRightIsBinaryConstant(BinaryExpressionNode expression)
        {
            var right = FoldConstantIntoToken(expression.Right);

            var leftType = EvaluateExpression(expression.Left);
            var constantRight = long.Parse(right.Value);

            ExpectIntTypeLeftTermOfSemiConstantExpression(leftType, expression.Position);
            ReportWhenDividingByZero(constantRight, expression.Operator.Kind == TokenKind.Slash, expression.Position);

            FunctionBuilder.EmitLoadConstantValue(constantRight, LowerDataType(leftType));

            return leftType;
        }

        private void ExpectIntTypeLeftTermOfSemiConstantExpression(DataType type, ModulePosition position)
        {
            if (!type.SolvedType.IsInt())
                ReportIncompatibleTypes(type.ToString(), "int literal", position);
        }

        private void ExpectIntTypeRightTermOfSemiConstantExpression(DataType type, ModulePosition position)
        {
            if (!type.SolvedType.IsInt())
                ReportIncompatibleTypes("int literal", type.ToString(), position);
        }

        private bool ReportWhenDividingByZero(long right, bool isDividing, ModulePosition position)
        {
            var dividingByZero = right == 0 && isDividing;

            if (dividingByZero)
                Tower.Report(position, "Dividing by '0' at compile time");

            return dividingByZero;
        }

        private void CheckLeftAndRightTypes(DataType left, DataType right, ModulePosition position)
        {
            if (AreNotCompatible(left, right))
                ReportIncompatibleTypes(left.ToString(), right.ToString(), position);
        }

        private void ReportIncompatibleTypes(string left, string right, ModulePosition position)
        {
            Tower.Report(position, $"Type mismatch: types '{left}' and '{right}' are incompatible");
        }

        private DataType EvaluateLeftBinaryIsConstant(BinaryExpressionNode expression)
        {
            var index = FunctionBuilder.CurrentIndex();
            var left = FoldConstantIntoToken(expression.Left);

            ContextTypesPushUndefined();

            var rightType = EvaluateExpression(expression.Right);

            ExpectIntTypeRightTermOfSemiConstantExpression(rightType, expression.Position);
            FunctionBuilder.EmitLoadConstantValue(long.Parse(left.Value), LowerDataType(rightType));

            ContextTypes.Pop();

            FunctionBuilder.MoveLastInstructionTo(index);
            return rightType;
        }

        private void EmitOperation(TokenKind op, DataType type)
        {
            FunctionBuilder.EmitInstruction((MIRInstructionKind)op, LowerDataType(type));
        }

        private Token FoldConstantIntoToken(INode opaque)
        {
            // make it better when introduce floating points
            return opaque switch
            {
                BadNode => Token.NewInfo(TokenKind.ConstantDigit, "1"),
                Token expression => expression,
                BinaryExpressionNode expression => FoldConstantBinaryExpressionIntoToken(expression),
                BooleanBinaryExpressionNode expression => FoldConstantBooleanBinaryExpressionIntoToken(expression),
                PrefixOperator expression => FoldConstantPrefixOperatorExpression(expression),
                _ => Token.NewInfo(TokenKind.Bad, "")
            };
        }

        private Token FoldConstantPrefixOperatorExpression(PrefixOperator expression)
        {
            var expr = FoldConstantIntoToken(expression.Expression);

            var type = ConstantTokenKindToDataType(expr.Kind);
            var resultType = type;
            var resultValue = expr.Value;

            switch (expression.Prefix.Kind)
            {
                case TokenKind.Plus:
                    ExpectIntTypeForPlusPrefixOperator(type, expression.Prefix.Position);
                    break;
                case TokenKind.Minus:
                    EvaluateConstantMinusPrefixOperator(expression, type, ref resultValue);
                    break;
                case TokenKind.Negation:
                    EvaluateConstantNegationPrefixOperator(expression, type, ref resultValue);
                    break;
                /*case TokenKind.Star:
                    break;
                case TokenKind.BooleanAND:
                    break;*/
                default:
                    ToImplement<object>(expression.Prefix.ToString(), nameof(EvaluatePrefixOperator));
                    break;
            }

            return new(DataTypeToConstantTokenKind(resultType), resultValue, expr.Position, false);
        }

        private void EvaluateConstantNegationPrefixOperator(PrefixOperator expression, DataType type, ref string resultValue)
        {
            FixAndCheckTypes(DataType.Bool, type, expression.Prefix.Position);
            if (bool.TryParse(resultValue, out var value))
                resultValue = (!value).ToString();
        }

        private static DataType ConstantTokenKindToDataType(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.ConstantString => DataType.String,
                TokenKind.ConstantDigit => DataType.Int32,
                TokenKind.ConstantChar => DataType.Char,
                TokenKind.ConstantFloatDigit => DataType.Float32,
            };
        }

        private static TokenKind DataTypeToConstantTokenKind(DataType type)
        {
            return type.SolvedType.Kind switch
            {
                TypeKind.String => TokenKind.ConstantString,
                TypeKind.Int32 => TokenKind.ConstantDigit,
                TypeKind.Char => TokenKind.ConstantChar,
                TypeKind.Float32 => TokenKind.ConstantFloatDigit,
            };
        }

        private Token FoldConstantBooleanBinaryExpressionIntoToken(BooleanBinaryExpressionNode expression)
        {
            return new(
                TokenKind.ConstantBoolean,
                FoldConstantBooleanExpressionIntoBool(
                    expression.Left,
                    expression.Right,
                    expression.Operator.Kind,
                    expression.Position).ToString(),
                expression.Position,
                false);
        }

        private Token FoldConstantBinaryExpressionIntoToken(BinaryExpressionNode expression)
        {
            return new(
                TokenKind.ConstantDigit,
                FoldConstants(
                    long.Parse(FoldConstantIntoToken(expression.Left).Value),
                    long.Parse(FoldConstantIntoToken(expression.Right).Value),
                    expression.Operator.Kind,
                    expression.Position).ToString(),
                expression.Position,
                false);
        }

        private long FoldConstants(long left, long right, TokenKind op, ModulePosition position)
        {
            return op switch
            {
                TokenKind.Plus => left + right,
                TokenKind.Minus => left - right,
                TokenKind.Star => left * right,
                TokenKind.Slash => EvaluateDivideConstants(left, right, position),
            };
        }

        private long EvaluateDivideConstants(long left, long right, ModulePosition position)
        {
            return !ReportWhenDividingByZero(right, true, position) ? left / right : 0;
        }

        private static bool IsConstantInt(INode node)
        {
            return
                (node is BadNode or Token {Kind: TokenKind.ConstantDigit})
                || (node is BinaryExpressionNode binary && IsConstantBinary(binary.Left, binary.Right));
        }

        private DataType EvaluateMemberNode(MemberNode expression)
        {
            return
                expression.Base is Token { Kind: TokenKind.Identifier } expressionBase
                && !GetLocalVariable(expressionBase.Value, out _) ?
                    EvaluateStaticMemberNode(expressionBase.Value, expression) :
                    EvaluateInstanceMemberNode(expression);
        }

        private DataType EvaluateStaticMemberNode(string baseValue, MemberNode expression)
        {
            var type = GetType(baseValue, expression.Base.Position);
            if (type is null || !GetStaticConstantFromType(type, expression.Member.Value, out var staticConstant))
                return ContextType;

            CompilationTower.Todo($"implement {nameof(EvaluateStaticMemberNode)}");
            throw new();
            // return staticConstant.Body;
        }

        private static bool GetStaticConstantFromType(TypeStatement type, string member, out VariableStatement constant)
        {
            for (int i = 0; i < type.BodyConstants.Count; i++)
            {
                constant = type.BodyConstants[i];
                if (constant.Name == member)
                    return true;
            }

            constant = null;
            return false;
        }

        private DataType EvaluateInstanceMemberNode(MemberNode expression)
        {
            var baseType = EvaluateExpression(expression.Base);
            if (!baseType.SolvedType.IsNewOperatorAllocable())
            {
                Tower.Report(expression.Base.Position, $"Type '{baseType}' is not accessible via operator '.'");
                return ContextType;
            }

            var structure = baseType.SolvedType.GetStruct();

            var type = GetFieldType(expression.Member.Value, structure, expression.Member.Position, out var index);
            LoadField(expression, structure, type, index);

            return type ?? ContextType;
        }

        private void LoadField(MemberNode expression, TypeStatement structure, DataType type, int index)
        {
            if (type is null)
            {
                Tower.Report(
                    expression.Member.Position,
                    $"Type '{structure.Name}' does not contain a definition for '{expression.Member.Value}'");
            }
            else
                FunctionBuilder.EmitLoadField(index, LowerDataType(type));
        }

        private void ExpectPublicFieldOrInternal(FieldNode field, TypeStatement type, ModulePosition position)
        {
            if (field.Modifier is not TokenKind.KeyPub && !ProcessingMethodOf(type))
                Tower.Report(position, $"Field '{field.Name}' is a private member of type '{type.Name}'");
        }

        private DataType EvaluateNodeTypeAllocation(TypeAllocationNode expression)
        {
            SolvedType type;
            if (!expression.IsAuto)
                type = expression.Name.SolvedType;
            else if (ContextTypeIsAmbiguousOrGet(expression.Position, out type))
                return DataType.Void;

            if (!type.IsNewOperatorAllocable())
            {
                Tower.Report(expression.Position, $"Unable to allocate type '{type}' via operator 'new'");
                return DataType.Solved(type);
            }

            return EvaluateStruct(expression, DataType.Solved(type));
        }

        private DataType EvaluateStruct(TypeAllocationNode expression, DataType type)
        {
            var structure = type.SolvedType.GetStruct();
            var assignedFields = new List<string>();

            ReportStaticTypeAllocationIfNeeded(expression, structure);

            FunctionBuilder.EmitLoadZeroinitializedStruct(LowerDataType(type));
            EvaluateTypeInitialization(expression, structure, assignedFields);
            // FunctionBuilder.EmitLoadValueFromPointer();

            return DataType.Solved(SolvedType.Struct(structure));
        }

        private void ReportStaticTypeAllocationIfNeeded(TypeAllocationNode expression, TypeStatement structure)
        {
            if (GetStructByteSize(structure) == 0)
                Tower.Report(expression.Position, "Unable to allocate a static type");
        }

        private int GetTypeByteSize(DataType type, ModulePosition position = default)
        {
            return type.SolvedType.Kind switch
            {
                TypeKind.Pointer => PointerSize(),
                TypeKind.String => PointerSize(),
                TypeKind.Char => 1,
                TypeKind.Int32 => 4,
                TypeKind.Int64 => 8,
                TypeKind.UInt8 => 1,
                TypeKind.UInt32 => 4,
                TypeKind.UInt64 => 8,
                TypeKind.Float32 => 4,
                TypeKind.Float64 => 8,
                TypeKind.Float128 => 16,
                TypeKind.Int8 => 1,
                TypeKind.Int16 => 2,
                TypeKind.UInt16 => 2,
                TypeKind.Bool => 1,
                TypeKind.Array => PointerSize(),
                TypeKind.DefinedType => GetStructByteSize(type.SolvedType.GetStruct(), position),
                TypeKind.GenericDefinedType => throw new NotImplementedException(),
                TypeKind.Void => 0,
                TypeKind.Unknown => PointerSize(),
                TypeKind.EnumError => throw new NotImplementedException(),
                TypeKind.Err => throw new NotImplementedException(),
                TypeKind.Option => throw new NotImplementedException(),
                TypeKind.Tuple => throw new NotImplementedException(),
            };
        }

        private int GetStructByteSize(TypeStatement type, ModulePosition position = default)
        {
            var result = 0;
            for (int i = 0; i < type.BodyFields.Count; i++)
                result += GetTypeByteSize(type.BodyFields[i].Type, position);

            if (IsValidPosition(position) && result == 0)
                Tower.Report(position, "Unable to calculate size of a static type");

            return result;
        }

        private static bool IsValidPosition(ModulePosition position)
        {
            return position.Position.End.Value > 0;
        }

        private int PointerSize()
        {
            // to fix
            return 64;
        }

        private void EvaluateTypeInitialization(TypeAllocationNode expression, TypeStatement structure, List<string> assignedFields)
        {
            for (var i = 0; i < expression.Body.Count; i++)
                EmitFieldInitialization(expression.Body[i], structure, assignedFields);
        }

        private void EmitFieldInitialization(FieldAssignmentNode field, TypeStatement structure, List<string> assignedFields)
        {
            if (assignedFields.Contains(field.Name))
            {
                Tower.Report(field.Position, $"Field '{field.Name}' assigned multiple times");
                return;
            }

            assignedFields.Add(field.Name);
            var fieldType = GetFieldType(field.Name, structure, field.Position, out var fieldIndex);

            if (fieldType is null)
            {
                Tower.Report(field.Position, $"Type '{structure.Name}' does not contain a definition for '{field.Name}'");
                return;
            }

            EvaluateFieldAssignmentInInitialization(field, fieldType, fieldIndex);
        }

        private void EvaluateFieldAssignmentInInitialization(FieldAssignmentNode field, DataType fieldType, int fieldIndex)
        {
            ContextTypes.Push(fieldType);

            FunctionBuilder.EmitDupplicate();
            FixAndCheckTypes(fieldType, EvaluateExpression(field.Body), field.Position);
            FunctionBuilder.EmitStoreField(fieldIndex, LowerDataType(fieldType));

            ContextTypes.Pop();
        }

        private bool ContextTypeIsAmbiguousOrGet(ModulePosition position, out SolvedType type)
        {
            type = ContextType.SolvedType;
            var isAmbiguous = type.IsAuto();
            if (isAmbiguous)
                Tower.Report(position, "Cannot infer ambiguous type");

            return isAmbiguous;
        }

        private DataType GetFieldType(string name, TypeStatement type, ModulePosition position, out int i)
        {
            for (i = 0; i < type.BodyFields.Count; i++)
            {
                var field = type.BodyFields[i];
                if (name == field.Name)
                {
                    ExpectPublicFieldOrInternal(field, type, position);
                    return field.Type;
                }
            }

            return null;
        }

        private DataType EvaluateIdentifier(string value, ModulePosition position)
        {
            if (!GetLocalVariable(value, out var allocation))
                allocation = ReportVariableNotDeclared(value, position);
            else
                CheckForConstantAssignmentAndLoadVariable(allocation);

            return allocation.Type;
        }

        private void CheckForConstantAssignmentAndLoadVariable(AllocationData allocation)
        {
            if (allocation.IsConst && LeftValueChecker.IsLeftValue)
                Tower.Report(LeftValueChecker.Position, "Constant allocation in left side of assignement");

            FunctionBuilder.EmitLoadLocal(allocation.StackIndex, LowerDataType(allocation.Type));
        }

        private AllocationData ReportVariableNotDeclared(string value, ModulePosition position)
        {
            var allocation = new AllocationData(0, ContextType, false);
            Tower.Report(position, $"Variable '{value}' is not declared");
            return allocation;
        }

        private bool GetLocalVariable(string value, out AllocationData allocation)
        {
            return CurrentScope.VirtualMemory.TryGetValue(value, out allocation);
        }

        private DataType EvaluateConstant(Token expression)
        {
            DataType result;
            object value;

            switch (expression.Kind)
            {
                case TokenKind.ConstantDigit:
                    result = CoercedOr(DataType.Int32);
                    value = long.Parse(expression.Value);
                    break;
                case TokenKind.ConstantBoolean:
                    result = DataType.Bool;
                    value = bool.Parse(expression.Value);
                    break;
                default:
                    ToImplement<object>(expression.Kind.ToString(), nameof(EvaluateConstant));
                    result = null;
                    value = null;
                    break;
            }

            FunctionBuilder.EmitLoadConstantValue(value, LowerDataType(result));

            return result;
        }

        private DataType CoercedOr(DataType or)
        {
            return ContextType.SolvedType.IsInt() ? ContextType : or;
        }

        private AllocationData DeclareVirtualMemorySymbol(string name, DataType type, ModulePosition position, bool isconst)
        {
            var localindex = GetAllocationsNumber();
            var allocation = new AllocationData(localindex, type, isconst);
            CheckForRedeclaration(name, position, allocation);

            FunctionBuilder.DeclareAllocation((MIRAllocationAttribute)Convert.ToInt32(isconst), LowerDataType(type));

            return allocation;
        }

        private void CheckForRedeclaration(string name, ModulePosition position, AllocationData allocation)
        {
            if (!CurrentScope.VirtualMemory.TryAdd(name, allocation))
                Tower.Report(position, $"Variable '{name}' is already declared");
        }

        private int GetAllocationsNumber()
        {
            return FunctionBuilder.GetAllocationsNumber();
        }

        private void GenerateFunction(FunctionStatement func, string irFunctionName)
        {
            FunctionBuilder = new(irFunctionName, LowerDataType(func.ReturnType), GetParameterTypes(func.ParameterList));
            CurrentFunction = func;
            CurrentScope = new(func.ReturnType, true, new());

            SetEntryBlock();

            ContextTypes.Push(func.ReturnType);
            AllocateParameters(func.ParameterList);
            GenerateBlock(func.Body);
            FunctionBuilder.EmitOptionalReturnVoid();
            ContextTypes.Pop();

            Module.DefineFunction(FunctionBuilder.Build());
        }

        private void SetEntryBlock()
        {
            FunctionBuilder.SwitchBlock(CreateBlock("entry"));
        }

        private void AllocateParameters(ParameterListNode parameters)
        {
            foreach (var parameter in parameters.Parameters)
                DeclareVirtualMemorySymbol(parameter.Name, parameter.Type, parameter.Position, true);
        }

        private void GenerateStruct(TypeStatement type)
        {
            CurrentType = type;

            foreach (var method in type.BodyMethods)
                GenerateFunction(method, $"{type.Name}.{method.Name}");

            CurrentType = null;
        }

        private bool ProcessingMethodOf(TypeStatement type)
        {
            return CurrentType == type;
        }

        private void CheckRecursiveType(TypeStatement type, List<string> illegalTypes)
        {
            illegalTypes.Add(type.Name);

            foreach (var field in type.BodyFields)
            {
                var fieldType = field.Type.SolvedType;
                if (!fieldType.IsStruct() &&
                    (!fieldType.IsPointer() || !fieldType.GetBaseElementType().SolvedType.IsStruct())) continue;
                
                var fieldStructType =
                (
                    fieldType.Kind is TypeKind.Pointer ?
                        ((DataType)fieldType.Base).SolvedType :
                        fieldType
                ).GetStruct();

                CheckIfThereIsARecursion(type, illegalTypes, field, fieldType, fieldStructType);
            }
        }

        private void CheckIfThereIsARecursion(
            TypeStatement type,
            List<string> illegalTypes,
            FieldNode field,
            SolvedType fieldType,
            TypeStatement fieldStructType)
        {
            if (illegalTypes.Contains(fieldType.ToString()))
            {
                Tower.Report(type.Position, "Recursive type");
                Tower.Report(field.Type.Position, $"Use '?{fieldType}' instead");
            }
            else
                CheckRecursiveType(fieldStructType, illegalTypes);
        }

        private void WalkDeclarations()
        {
            FunctionStatement entrypoint = null;
            foreach (var symbol in Tower.Symbols.GetCache())
                RecognizeSymbol(symbol.Value, ref entrypoint);

            CheckEntryPoint(entrypoint);
        }

        private void CheckEntryPoint(FunctionStatement entryPoint)
        {
            if (entryPoint is null)
                ReportMissingEntryPoint();
            else
            {
                if (entryPoint.ParameterList.Length > 0)
                    Tower.Report(entryPoint.Position, "Entrypoint cannot have parameters");
                if (entryPoint.Generics.Count > 0)
                    Tower.Report(entryPoint.Position, "Entrypoint cannot have generic parameters");
                if (entryPoint.ReturnType.UnsolvedType.Kind is not TypeKind.Void)
                    Tower.Report(entryPoint.Position, "Entrypoint cannot return a value");
                if (entryPoint.Modifier is TokenKind.KeyPub)
                    Tower.Report(entryPoint.Position, "Entrypoint cannot have a public modifier");
            }
        }

        private void ReportMissingEntryPoint()
        {
            if (Tower.IsExpectingEntryPoint())
                Tower.Report(null, 0, "Missing entrypoint");
        }

        private void RecognizeSymbol(ISymbol value, ref FunctionStatement entrypoint)
        {
            switch (value)
            {
                case TypeStatement structSymbol:
                    CheckRecursiveType(structSymbol, new());
                    GenerateStruct(structSymbol);
                    break;
                case FunctionStatement funcSymbol:
                    if (funcSymbol.Name is EntryPointName)
                        entrypoint = funcSymbol;

                    GenerateFunction(funcSymbol, funcSymbol.Name);
                    break;
                default:
                    ToImplement<object>(value.ToString(), nameof(RecognizeSymbol));
                    break;
            }
        }
    }
}