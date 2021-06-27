using Mug.Compilation;
using Mug.Generator.IR;
using Mug.Generator.IR.Builder;
using Mug.Tokenizer;
using Mug.Parser;
using Mug.Parser.AST;
using Mug.Parser.AST.Statements;
using Mug.Parser.ASTLowerer;
using Mug.Symbols;
using Mug.TypeResolution;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

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
        public int _currentCycleConditionBlockIndex = -1;
        private int _currentScopeEndBlockIndex = -1;

        public MIRGenerator(CompilationTower tower) : base(tower)
        {
            Lowerer = new(Tower);
        }

        public MIR Generate()
        {
            SetUpGlobals();
            DeclareBuiltinStructs();
            WalkThroughDeclarations();
            Tower.CheckDiagnostic();

            CheckForConflicts();

            return Module.Build();
        }

        private void DeclareBuiltinStructs()
        {
            // Module.DefineStruct(LowerStruct());
        }

        private void AddToGlobalResources()
        {
            Tower.Unit.GlobalResources.Add((Tower.Unit.Paths.First(), Tower.Symbols));
        }

        private void CheckForConflicts()
        {
            if (!IsMainUnit())
                return;

            AddToGlobalResources();

            for (int i = 0; i < Tower.Unit.GlobalResources.Count; i++)
                CycleDoublyGlobalResources(i, Tower.Unit.GlobalResources[i]);
        }

        private void CycleDoublyGlobalResources(int i, (string Path, SymbolTable Symbols) resourceI)
        {
            for (int j = 0; j < Tower.Unit.GlobalResources.Count; j++)
            {
                var resourceJ = Tower.Unit.GlobalResources[j];

                if (i == j)
                    continue;

                if (CheckForConflictsBetweenSymbolTables(resourceI, resourceJ, out var position, out var symbol, out var moduleI, out var moduleJ))
                    Tower.Report(position, $"Detected conflict with symbol '{symbol}' between modules '{moduleI}' and '{moduleJ}'");
            }
        }

        private static bool CheckForConflictsBetweenSymbolTables(
            (string Path, SymbolTable Symbols) resourceI,
            (string Path, SymbolTable Symbols) resourceJ,
            out ModulePosition position,
            out string symbol,
            out string moduleI,
            out string moduleJ)
        {
            moduleI = Path.GetFileNameWithoutExtension(resourceI.Path);
            moduleJ = Path.GetFileNameWithoutExtension(resourceJ.Path);

            foreach (var symbolI in resourceI.Symbols.GetCache())
                if (resourceJ.Symbols.SymbolIsDeclared(symbolI.Key))
                {
                    symbol = symbolI.Key;
                    position = symbolI.Value.Position;
                    return true;
                }

            position = default;
            symbol = null;
            return false;
        }

        private void SetUpGlobals()
        {
        }

        private static DataType[] GetParameterTypes(ParameterNode[] parameters)
        {
            var result = new DataType[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
                result[i] = parameters[i].Type;

            return result;
        }

        /*private DataType (DataType type)
        {
            var kind = type.SolvedType.Kind;
            return kind switch
            {
                TypeKind.Auto
                or TypeKind.Undefined => new(),

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

                TypeKind.Pointer => DataType.VoidPointer,

                TypeKind.String => DataType.String,

                _ => ToImplement<DataType>(kind.ToString(), nameof())
            };
        }*/

        private static MIRStructure LowerStruct(TypeStatement type)
        {
            return new MIRStructure(type.IsPacked, type.Name, LowerStructBody(type.BodyFields));
        }

        private static DataType[] LowerStructBody(List<FieldNode> bodyFields)
        {
            var result = new DataType[bodyFields.Count];
            for (int i = 0; i < bodyFields.Count; i++)
                result[i] = bodyFields[i].Type;

            return result;
        }

        /*private static int GetIntBitSize(TypeKind kind)
        {
            var value = kind.ToString();
            return value.Last() == '8' ? 8 : int.Parse(value.Substring(value.Length - 2, 2));
        }*/

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
                case LoopManagementStatement statement:
                    GenerateLoopManagementStatement(statement);
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

        private void GenerateLoopManagementStatement(LoopManagementStatement statement)
        {
            if (_currentCycleConditionBlockIndex == -1)
                Tower.Report(statement.Position, $"'{statement.Kind}' statement cannot be used outside cycles");
            else
                FunctionBuilder.EmitJump(statement.Kind is TokenKind.KeyContinue ? _currentCycleConditionBlockIndex : _currentScopeEndBlockIndex);
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
            var oldScopeProperties = GetScopePropertiesAndSetNew(conditionBlock.Index, endBlock.Index);

            EvaluateForLoopConditionAndGetConditionBlock(statement.LeftExpression, statement.ConditionExpression, conditionBlock, thenBlock, endBlock);

            SwitchBlock(thenBlock);
            ContextTypes.Push(DataType.Void);
            GenerateBlock(statement.Body);
            ContextTypes.Pop();

            RecognizeStatement(statement.RightExpression, false);
            FunctionBuilder.EmitJump(conditionBlock.Index);

            SwitchBlock(endBlock);
            RestoreOldScopeProperties(oldScopeProperties);
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
            var oldScopeProperties = GetScopePropertiesAndSetNew(conditionBlock.Index, endBlock.Index);

            FunctionBuilder.EmitJump(conditionBlock.Index);

            SwitchBlock(conditionBlock);
            EvaluateConditionInConditionalStatement(statement.Expression, then, endBlock);

            SwitchBlock(then);
            ContextTypes.Push(DataType.Void);
            GenerateNodeBlockInExpression(statement.Body);
            ContextTypes.Pop();

            FunctionBuilder.EmitJump(conditionBlock.Index);

            SwitchBlock(endBlock);
            RestoreOldScopeProperties(oldScopeProperties);
        }

        private void RestoreOldScopeProperties((int, int) oldScopeProperties)
        {
            (_currentCycleConditionBlockIndex, _currentScopeEndBlockIndex) = oldScopeProperties;
        }

        private (int, int) GetScopePropertiesAndSetNew(int cycleConditionBlockIndex, int scopeEndBlockIndex)
        {
            var old = GetScopeProperties();
            (_currentCycleConditionBlockIndex, _currentScopeEndBlockIndex) = (cycleConditionBlockIndex, scopeEndBlockIndex);

            return old;
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

        private (int, int) GetScopeProperties()
        {
            return (_currentCycleConditionBlockIndex, _currentScopeEndBlockIndex);
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
                FunctionBuilder.EmitReturn((type));
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

            FunctionBuilder.EmitReturn(CurrentFunction.ReturnType);
        }

        private void GenerateAssignmentStatement(AssignmentStatement statement)
        {
            Lowerer.LowerAssignmentStatementOperator(ref statement);

            ContextTypesPushUndefined();
            SetLeftValueChecker(statement.Position);

            var firstInstructionIndex = FunctionBuilder.CurrentIndex();
            var variable = EvaluateExpression(statement.Name);

            if (variable.SolvedType.IsUndefined())
                return;

            var (firstInstruction, lastInstruction) = EvaluateLeftSideOfAssignment(statement, variable, firstInstructionIndex);

            var expressiontype = EvaluateExpression(statement.Body);

            FixAndCheckTypes(variable, expressiontype, statement.Body.Position);

            ContextTypes.Pop();

            FunctionBuilder.EmitInstruction(firstInstruction);

            if (!firstInstruction.Equals(lastInstruction) && firstInstruction.Kind is not MIRInstructionKind.StorePointer)
                FunctionBuilder.EmitInstruction(lastInstruction);
        }

        private (MIRInstruction, MIRInstruction) EvaluateLeftSideOfAssignment(AssignmentStatement statement, DataType variable, int firstInstructionIndex)
        {
            ClearLeftValueChecker();

            ContextTypes.Pop();

            var lastInstruction = FunctionBuilder.GetInstructionAt(firstInstructionIndex);
            var firstInstruction = FunctionBuilder.PopLastInstruction();

            if (!IsConvertibleToLeftExpressionInstruction(firstInstruction.Kind))
                Tower.Report(statement.Name.Position, $"Expression in left side of assignment");

            ContextTypes.Push(variable);

            firstInstruction.Kind = GetLeftExpressionInstruction(firstInstruction.Kind);
            lastInstruction.Kind = GetLeftExpressionInstruction(lastInstruction.Kind);

            return (firstInstruction, lastInstruction);
        }

        private static bool IsConvertibleToLeftExpressionInstruction(MIRInstructionKind kind)
        {
            return kind is MIRInstructionKind.LoadLocal or MIRInstructionKind.LoadField or MIRInstructionKind.LoadValueFromPointer;
        }

        private void ClearLeftValueChecker()
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

            var contextType = GetFirstIfNotAutoOrSecond(statement.Type, DataType.Undefined);
            ContextTypes.Push(contextType);

            var expressiontype = EvaluateExpression(!statement.IsAssigned ? GetDefaultValueOf(statement.Type, statement.Position) : statement.Body);

            FixAndCheckTypes(statement.Type, expressiontype, statement.Body.Position);
            var allocation = DeclareVirtualMemorySymbol(statement.Name, statement.Type, statement.Position, statement.IsConst);

            FunctionBuilder.EmitStoreLocal(allocation.StackIndex, (allocation.Type));

            ContextTypes.Pop();
        }

        private static DataType GetFirstIfNotAutoOrSecond(DataType first, DataType second)
        {
            return !first.SolvedType.IsAuto() ? first : second;
        }

        private void CheckVariable(VariableStatement statement)
        {
            if (statement.IsAssigned) return;

            if (statement.IsConst)
                Tower.Report(statement.Position, "A constant declaration requires a body");
            if (statement.Type.SolvedType.Kind is TypeKind.Auto)
                Tower.Report(statement.Position, "Type notation needed");
        }

        private INode GetDefaultValueOf(DataType type, ModulePosition position)
        {
            return type.SolvedType.Kind switch
            {
                TypeKind.Undefined
                or TypeKind.Auto
                or TypeKind.Void => new BadNode(position),

                TypeKind.Pointer => error("Uninitialized pointer"),

                TypeKind.String => token(TokenKind.ConstantString, ""),

                TypeKind.Int8
                or TypeKind.Int16
                or TypeKind.Int32
                or TypeKind.Int64
                or TypeKind.UInt8
                or TypeKind.UInt16
                or TypeKind.UInt32
                or TypeKind.UInt64 => token(TokenKind.ConstantDigit, "0"),

                TypeKind.Float32
                or TypeKind.Float64
                or TypeKind.Float128 => token(TokenKind.ConstantFloatDigit, "0"),

                TypeKind.Enum => GetDefaultOfEnum(type, position),

                TypeKind.Char => token(TokenKind.ConstantChar, "\0"),

                TypeKind.Bool => token(TokenKind.ConstantBoolean, "false"),

                TypeKind.CustomType => GetDefaultOfStruct(type.SolvedType.GetStruct(), position),

                TypeKind.Option => GetDefaultOfOption(type, position),

                _ => toImpl(),
            };

            INode error(string message)
            {
                Tower.Report(position, message);
                return new BadNode(position);
            }

            Token token(TokenKind kind, string value)
            {
                return new Token(kind, value, position, false);
            }

            INode toImpl()
            {
                ToImplement<object>(type.SolvedType.Kind.ToString(), nameof(GetDefaultValueOf));
                return null;
            }
        }

        private static INode GetDefaultOfEnum(DataType type, ModulePosition position)
        {
            return new MemberNode
            {
                Base = new Token(TokenKind.Identifier, type.ToString(), position, false),
                Member = new Token(TokenKind.Identifier, type.SolvedType.GetEnum().Body.First().Name, position, false),
                Position = position
            };
        }

        private INode GetDefaultOfOption(DataType type, ModulePosition position)
        {
            var successType = type.SolvedType.GetOption().Success;
            var def = GetDefaultValueOf(successType, position);

            var call = new CallStatement
            {
                Name = new Token(TokenKind.Identifier, "some", position, false),
                IsBuiltIn = true,
                Position = position
            };

            call.Parameters.Add(def);
            call.Generics.Add(successType);

            return call;
        }

        private static INode GetDefaultOfStruct(TypeStatement type, ModulePosition position)
        {
            return new TypeAllocationNode
            {
                Name = DataType.Solved(SolvedType.Struct(type)),
                Position = position
            };
        }

        private void FixAndCheckTypes(DataType expected, DataType gottype, ModulePosition position)
        {
            FixAuto(expected, gottype);

            if (expected.SolvedType.Kind is TypeKind.Undefined
                || gottype.SolvedType.Kind is TypeKind.Undefined)
                return;

            MakeConvertions(ref expected, ref gottype, position);

            if (AreNotCompatible(expected, gottype))
                Tower.Report(position, $"Type mismatch: expected type '{expected}', but got '{gottype}'");
        }

        private void MakeConvertions(ref DataType expected, ref DataType gottype, ModulePosition position)
        {
        }

        private static bool AreNotCompatible(DataType expected, DataType gottype)
        {
            var rightsolved = expected.SolvedType;
            var leftsolved = gottype.SolvedType;

            return
                rightsolved.Kind != leftsolved.Kind
                || (rightsolved.Base is not null
                    && !rightsolved.Base.Equals(leftsolved.Base));
        }

        private static void FixAuto(DataType type, DataType expressiontype)
        {
            if (type.SolvedType.Kind is TypeKind.Auto)
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
            if (expression.Prefix.Kind is TokenKind.Apersand)
                return EvaluateApersandPrefixOperator(expression);
            if (expression.Prefix.Kind is TokenKind.Star)
                return EvaluateStarPrefixOperator(expression.Expression);

            var expr =
                !IsConstant(expression.Expression) ?
                    expression.Expression :
                    FoldConstantIntoToken(expression.Expression);


            var type = EvaluateExpression(expr);
            var result = ContextType;

            switch (expression.Prefix.Kind)
            {
                case TokenKind.Plus:
                    ExpectIntTypeForPlusPrefixOperator(type, expression.Prefix.Position);
                    break;
                case TokenKind.Minus:
                    EvaluateMinusPrefixOperator(expression, type);
                    break;
                case TokenKind.Negation:
                    EvaluateNegationPrefixOperator(expression, type);
                    break;
                default:
                    ToImplement<object>(expression.Prefix.ToString(), nameof(EvaluatePrefixOperator));
                    break;
            }

            return result;
        }

        private DataType EvaluateApersandPrefixOperator(PrefixOperator expression)
        {
            SetLeftValueChecker(expression.Prefix.Position);

            var first = FunctionBuilder.CurrentIndex();
            var type = EvaluateExpression(expression.Expression);
            var last = FunctionBuilder.CurrentIndex();

            var instructions = FunctionBuilder.PopUntil(first, last);

            if (!IsConvertibleToLeftExpressionInstruction(instructions.Last().Kind))
            {
                Tower.Report(expression.Expression.Position, $"Expected a local variable");
                return ContextType;
            }

            EmitConverted(instructions);

            ClearLeftValueChecker();
            return DataType.Pointer(type);
        }

        private void EmitConverted(MIRInstruction[] instructions)
        {
            foreach (var instruction in instructions)
                FunctionBuilder.EmitInstruction(ConvertKindToLoadPointer(instruction));
        }

        private static MIRInstruction ConvertKindToLoadPointer(MIRInstruction instruction)
        {
            instruction.Kind += MIRInstructionKind.LoadLocalAddress - MIRInstructionKind.LoadLocal;
            return instruction;
        }

        private DataType EvaluateStarPrefixOperator(INode expression)
        {
            var leftValueChecker = ClearLeftValueCheckerAndGetOld();
            var type = EvaluateExpression(expression);

            if (!type.SolvedType.IsPointer())
            {
                Tower.Report(expression.Position, $"Expected pointer type");
                return ContextType;
            }

            FunctionBuilder.EmitLoadValueFromPointer((type.SolvedType.GetBaseElementType()));
            LeftValueChecker = leftValueChecker;
            return (DataType)type.SolvedType.Base;
        }

        private (bool, ModulePosition) ClearLeftValueCheckerAndGetOld()
        {
            var result = LeftValueChecker;
            ClearLeftValueChecker();
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
            FunctionBuilder.EmitNeg((type));
        }

        private void EvaluateConstantMinusPrefixOperator(PrefixOperator expression, DataType type, ref string resultValue)
        {
            ExpectSignedIntForMinusPrefixOperator(type, expression.Prefix.Position);
            if (long.TryParse(resultValue, out var value))
                resultValue = (-value).ToString();
        }

        private void EvaluateNegationPrefixOperator(PrefixOperator expression, DataType type)
        {
            FixAndCheckTypes(DataType.Bool, type, expression.Prefix.Position);
            FunctionBuilder.EmitNeg((DataType.Bool));
        }

        private void ExpectSignedIntForMinusPrefixOperator(DataType type, ModulePosition position)
        {
            if (!type.SolvedType.IsSignedInt())
                Tower.Report(position, $"Unable to apply operator '-' over type '{type}'");
        }

        private DataType EvaluateToken(Token expression)
        {
            return
                expression.Kind is TokenKind.Identifier ?
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

            FunctionBuilder.EmitLoadConstantValue(constantRight, (leftType));

            return leftType;
        }

        private DataType EvaluateLeftBooleanBinaryIsConstant(BooleanBinaryExpressionNode expression)
        {
            var index = FunctionBuilder.CurrentIndex();
            var left = FoldConstantIntoToken(expression.Left);

            ContextTypesPushUndefined();

            var rightType = EvaluateExpression(expression.Right);

            ExpectIntTypeRightTermOfSemiConstantExpression(rightType, expression.Position);
            FunctionBuilder.EmitLoadConstantValue(long.Parse(left.Value), (rightType));

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

            FunctionBuilder.EmitLoadConstantValue(result, (DataType.Bool));
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
            var func = EvaluateFunctionName(expression.Name, ref parameters, out var typePrefix);
            expression.Parameters = parameters;

            if (func is null)
                return ContextType;

            EvaluateCallParameters(expression, func, ProcessPrefix(typePrefix));

            return func.ReturnType;
        }

        private static string ProcessPrefix(string typePrefix)
        {
            return typePrefix + (typePrefix is null ? "" : ".");
        }

        private DataType EvaluateNodeCallBuiltIn(CallStatement expression, bool isStatement)
        {
            if (expression.Name is not Token name)
            {
                Tower.Report(expression.Position, "Unable to call builtin function");
                return ContextType;
            }

            var type = ContextType;

            switch (name.Value)
            {
                case "size":
                    WarnWhenStatement();
                    type = EvaluateBuiltInSize(expression);
                    break;
                case "u8" or "u16" or "u32" or "u64"
                or "i8" or "i16" or "i32" or "i64":
                    WarnWhenStatement();
                    type = EvaluateBuiltInIntCast(expression, name.Value);
                    break;
                case "bool":
                    WarnWhenStatement();
                    type = EvaluateBuiltInBoolCast(expression);
                    break;
                case "exit":
                    type = EvaluateBuiltInExit(expression);
                    break;
                case "name":
                    WarnWhenStatement();
                    type = EvaluateBuiltInName(expression);
                    break;
                case "none":
                    WarnWhenStatement();
                    type = EvaluateBuiltInNone(expression);
                    break;
                case "some":
                    WarnWhenStatement();
                    type = EvaluateBuiltInSome(expression);
                    break;
                case "alloc":
                    WarnWhenStatement();
                    type = EvaluateBuiltInAlloc(expression);
                    break;
                case "drop":
                    type = EvaluateBuiltInDrop(expression);
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

        private DataType EvaluateBuiltInSome(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 1);
            var value = expression.Parameters.FirstOrDefault();
            if (value is null)
                return ContextType;

            var first = FunctionBuilder.CurrentIndex();

            ContextTypes.Push(OptionBaseTypeOrDefault().Success);
            var type = EvaluateExpression(value);
            var result = DataType.Option(OptionBaseTypeOrDefault().Error, type);
            ContextTypes.Pop();

            FunctionBuilder.EmitLoadZeroinitializedStruct(result);
            FunctionBuilder.MoveLastInstructionTo(first);

            FunctionBuilder.EmitStoreField(1, type);
            FunctionBuilder.EmitLoadConstantValue(1L, DataType.Bool);
            FunctionBuilder.EmitStoreField(0, DataType.Bool);

            return result;
        }

        private (DataType Error, DataType Success) OptionBaseTypeOrDefault()
        {
            return
                ContextType.SolvedType.IsOption() ?
                    ContextType.SolvedType.GetOption() :
                    (DataType.Primitive(TypeKind.EmptyEnum), DataType.Undefined);
        }

        private DataType EvaluateBuiltInNone(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 0, 1);
            var value = expression.Parameters.FirstOrDefault();

            if (!ContextType.SolvedType.IsOption())
            {
                Tower.Report(expression.Position, "Unable to infer type from a non-option context type");
                return ContextType;
            }

            var errortype = ContextType.SolvedType.GetOption().Error;

            if (value is null == errortype.Kind is TypeKind.Enum)
                Tower.Report(expression.Position, "Expected value or empty");

            FunctionBuilder.EmitLoadZeroinitializedStruct(ContextType);
            if (value is not null)
                EvaluateExpression(value);
            else
                FunctionBuilder.EmitLoadConstantValue(0L, errortype);
            FunctionBuilder.EmitStoreField(0, errortype);

            return ContextType;
        }

        private DataType EvaluateBuiltInBoolCast(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 1);
            var value = expression.Parameters.FirstOrDefault() ?? GetDefaultValueOf(DataType.Int32, expression.Position);
            var type = EvaluateExpression(value);

            if (!type.SolvedType.IsInt())
                Tower.Report(value.Position, "Expected int type");

            FunctionBuilder.EmitCastIntToInt(DataType.Bool);

            return DataType.Bool;
        }

        private DataType EvaluateBuiltInDrop(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 1);
            var value = expression.Parameters.FirstOrDefault();
            var expressionType = EvaluateExpression(value);

            TryDeclareExternFree(out var loweredReturnType, out var parameterType);

            if (value is null || !expressionType.SolvedType.IsPointer())
            {
                Tower.Report(expression.Parameters.Position, $"Expected pointer");
                return DataType.Void;
            }

            FunctionBuilder.EmitCastPointerToPointer(parameterType);
            FunctionBuilder.EmitCall("free", loweredReturnType);

            return DataType.Void;
        }

        private void TryDeclareExternFree(out DataType loweredReturnType, out DataType parameterType)
        {
            loweredReturnType = DataType.Void;
            parameterType = DataType.Pointer(DataType.UInt8);
            TryDeclareExternPrototype("free", loweredReturnType, parameterType);
        }

        private DataType EvaluateBuiltInAlloc(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0, 1);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 0, 1);

            if (expression.Parameters.Count == expression.Generics.Count && expression.Parameters.Count == 0)
            {
                Tower.Report(expression.Name.Position, $"Expected generic parameter or value");
                return ContextType;
            }

            var value = expression.Parameters.FirstOrDefault() ?? GetDefaultValueOf(expression.Generics.First(), expression.Parameters.Position);
            var contextType = expression.Generics.FirstOrDefault() ?? PointerBaseTypeOr(ContextType, DataType.Undefined);

            var first = FunctionBuilder.CurrentIndex();
            var expressionType = EvaluateMallocExpression(contextType, value);
            var byteSize = GetTypeByteSize(expressionType);

            if (expression.Generics.Count == 1)
                FixAndCheckTypes(contextType, expressionType, value.Position);

            var result = DataType.Pointer(expressionType);
            var loweredResult = (result);

            EmitMallocCall(byteSize, first, loweredResult);

            return result;
        }

        private static DataType PointerBaseTypeOr(DataType type, DataType or)
        {
            return
                type.SolvedType.IsPointer() ?
                    type.SolvedType.GetBaseElementType() :
                    or;
        }

        private void EmitMallocCall(long byteSize, int first, DataType loweredResult)
        {
            TryDeclareExternMalloc(out var loweredReturnType, out var loweredParameterType);

            FunctionBuilder.EmitLoadConstantValue(byteSize, loweredParameterType);
            FunctionBuilder.MoveLastInstructionTo(first++);
            FunctionBuilder.EmitCall("malloc", loweredReturnType);
            FunctionBuilder.MoveLastInstructionTo(first++);

            FunctionBuilder.EmitCastPointerToPointer(loweredResult);
            FunctionBuilder.MoveLastInstructionTo(first++);
            FunctionBuilder.EmitDupplicate();
            FunctionBuilder.MoveLastInstructionTo(first);

            FunctionBuilder.EmitStorePointer();
        }

        private DataType EvaluateMallocExpression(DataType contextType, INode value)
        {
            ContextTypes.Push(contextType);
            var expressionType = EvaluateExpression(value);
            ContextTypes.Pop();
            return expressionType;
        }

        private void TryDeclareExternMalloc(out DataType loweredReturnType, out DataType loweredParameterType)
        {
            loweredReturnType = (DataType.Pointer(DataType.UInt8));
            loweredParameterType = DataType.UInt64;

            TryDeclareExternPrototype("malloc", loweredReturnType, loweredParameterType);
        }

        private DataType EvaluateBuiltInExit(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 0, 1);
            var value = expression.Parameters.FirstOrDefault() ?? Token.NewInfo(TokenKind.ConstantDigit, "0");
            var type = EvaluateExpression(value);
            var result = ContextType;

            FixAuto(result, DataType.Int32);

            TryDeclareExternExit(result, out var loweredReturnType, out var parameterType);

            if (!type.SolvedType.IsInt())
                ReportExpectedValueOfTypeInt(value.Position);
            else if (type.SolvedType.Kind is not TypeKind.Int32)
                FunctionBuilder.EmitCastIntToInt(parameterType);

            FunctionBuilder.EmitCall("exit", loweredReturnType);

            return result;
        }

        private void TryDeclareExternExit(DataType result, out DataType loweredReturnType, out DataType parameterType)
        {
            loweredReturnType = (result);
            parameterType = DataType.Int32;

            TryDeclareExternPrototype("exit", loweredReturnType, parameterType);
        }

        private void TryDeclareExternPrototype(string name, DataType type, params DataType[] parameterTypes)
        {
            if (!Module.FunctionPrototypeIsDeclared(name))
                Module.DefineFunctionPrototype(name, type, parameterTypes);
        }

        private void ReportExpectedValueOfTypeInt(ModulePosition position)
        {
            Tower.Report(position, "Expected a value of type 'int'");
        }

        private DataType EvaluateBuiltInName(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 0);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 1);
            var value = expression.Parameters.FirstOrDefault();

            if (value is null)
                return ContextType;

            if (value is not Token referenceName)
            {
                Tower.Report(value is not null ? value.Position : expression.Name.Position, "Expected a member's name");
                return ContextType;
            }

            var item =
                Tower.Symbols.GetSymbol<ISymbol>(referenceName.Value, value.Position, "function neither a type") ??
                new FunctionStatement();
            
            EmitLoadConstantString(item.ToString());
            return DataType.String;
        }

        private void EmitLoadConstantString(string value)
        {
            TryDeclareExternPrototype("$create_str", DataType.String, DataType.Pointer(DataType.UInt8), DataType.UInt64);
            FunctionBuilder.EmitLoadConstantString(value);
        }

        private DataType EvaluateBuiltInIntCast(CallStatement expression, string name)
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
            FunctionBuilder.EmitCastIntToInt((type));
            return type;
        }

        private static DataType IntTypeStringToMIRType(string name)
        {
            var bitSize = int.Parse(name[1..]);
            var isUnsigned = Convert.ToByte(name.First() is 'u');
            return DataType.Primitive((TypeKind)(bitSize / 2 + isUnsigned));
        }

        private DataType EvaluateBuiltInSize(CallStatement expression)
        {
            ExpectGenericsNumber(expression.Generics, expression.Position, 1);
            ExpectParametersNumber(expression.Parameters, expression.Parameters.Position, 0);
            var type = expression.Generics.FirstOrDefault();
            var returnType = CoercedOr(DataType.Int32);

            if (type is not null)
                FunctionBuilder.EmitLoadConstantValue(GetTypeByteSize(type, type.Position), (returnType));

            return returnType;
        }

        private void ExpectParametersNumber(NodeBuilder parameters, ModulePosition position, params int[] expectedNumbers)
        {
            if (!expectedNumbers.Contains(parameters.Count))
                ReportIncorrectNumberOfElements(parameters.Count, position, expectedNumbers);
        }

        private void ReportIncorrectNumberOfElements(int count, ModulePosition position, int[] expectedNumbers, string kind = null)
        {
            Tower.Report(position, $"Expected '{string.Join("', '", expectedNumbers)}' {kind}parameters, but got '{count}'");
        }

        private void ExpectGenericsNumber(List<DataType> generics, ModulePosition position, params int[] expectedNumbers)
        {
            if (!expectedNumbers.Contains(generics.Count))
                ReportIncorrectNumberOfElements(generics.Count, position, expectedNumbers, "generic ");
        }

        private FunctionStatement EvaluateFunctionName(INode functionName, ref NodeBuilder parameters, out string typePrefix)
        {
            typePrefix = null;
            var funcSymbol = functionName switch
            {
                Token name => CheckAndSearchForFunctionOrStaticMethod(name),
                MemberNode name => EvaluateBaseFunctionName(name, ref parameters, out typePrefix),
                _ => FunctionBaseNameToImplement(functionName),
            };

            return funcSymbol;
        }

        private FunctionStatement FunctionBaseNameToImplement(INode functionName)
        {
            Tower.Report(functionName.Position, "Invalid construction");
            throw new();
        }

        private FunctionStatement EvaluateBaseFunctionName(MemberNode name, ref NodeBuilder parameters, out string typePrefix)
        {
            return
                name.Base is Token baseToken ?
                    EvaluateTokenBaseFunctionName(name, parameters, baseToken, out typePrefix) :
                    EvaluateExpressionBaseFunctionName(name, parameters, out typePrefix);
        }

        private FunctionStatement EvaluateExpressionBaseFunctionName(MemberNode name, NodeBuilder parameters, out string typePrefix)
        {
            typePrefix = null;
            parameters.Prepend(name.Base);

            var type = GetExpressionType(name.Base);

            if (!type.SolvedType.IsStruct())
                return ReportPrimitiveCannotHaveMethods(name.Base.Position);

            var structModel = type.SolvedType.GetStruct();

            typePrefix = structModel.Name;
            return SearchForMethod(name.Member.Value, structModel, name.Member.Position);
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

        private FunctionStatement EvaluateTokenBaseFunctionName(MemberNode name, NodeBuilder parameters, Token baseToken, out string typeName)
        {
            typeName = null;

            if (baseToken.Kind is not TokenKind.Identifier)
                return ReportPrimitiveCannotHaveMethods(baseToken.Position);

            if (GetLocalVariable(baseToken.Value, out var allocation))
            {
                if (!allocation.Type.SolvedType.IsStruct())
                    return ReportPrimitiveCannotHaveMethods(baseToken.Position);

                parameters.Prepend(baseToken);
                typeName = allocation.Type.SolvedType.GetStruct().Name;
            }
            else
                typeName = baseToken.Value;

            var function = SearchForMethod(name.Member.Value, GetType(typeName, baseToken.Position), name.Member.Position);

            return function;
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

            Tower.Report(position, $"No method '{value}' declared in type '{type.Name}'");
            return null;
        }

        private void ExpectPublicMethodOrInternal(FunctionStatement method, TypeStatement type, ModulePosition position)
        {
            if (method.Modifier is not TokenKind.KeyPub && !ProcessingMethodOf(type))
                Tower.Report(position, $"Method '{method.Name}' is a private member of type '{type.Name}'");
        }

        private void EvaluateCallParameters(CallStatement expression, FunctionStatement func, string typePrefix)
        {
            ReportFewParameters(expression, func);

            for (var i = 0; i < expression.Parameters.Count; i++)
                CheckAndEvaluateParameter(func, i, expression.Parameters[i]);

            FunctionBuilder.EmitCall(typePrefix + func.Name, (func.ReturnType));
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
            var prototypeParameterType = func.ParameterList[i].Type;
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
                return DefaultFunctionStatement(name);
            }

            if (ProcessingMethod() && IsInternalMethod(name.Value, out var method))
                return method;

            return Tower.Symbols.GetSymbol<FunctionStatement>(name.Value, name.Position, "function");
        }

        private static FunctionStatement DefaultFunctionStatement(Token name)
        {
            return new() { Name = name.Value, ReturnType = DataType.Int32 };
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
            FunctionBuilder.EmitLoadConstantValue(constant, (type));
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

            FunctionBuilder.EmitLoadConstantValue(constantRight, (leftType));

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
            FunctionBuilder.EmitLoadConstantValue(long.Parse(left.Value), (rightType));

            ContextTypes.Pop();

            FunctionBuilder.MoveLastInstructionTo(index);
            return rightType;
        }

        private void EmitOperation(TokenKind op, DataType type)
        {
            FunctionBuilder.EmitInstruction((MIRInstructionKind)op, (type));
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
                (node is BadNode or Token { Kind: TokenKind.ConstantDigit })
                || (node is BinaryExpressionNode binary && IsConstantBinary(binary.Left, binary.Right));
        }

        private DataType EvaluateMemberNode(MemberNode expression)
        {
            if (expression.Base is Token { Kind: TokenKind.Identifier } expressionBase
                && !GetLocalVariable(expressionBase.Value, out _))
                return EvaluateStaticMemberNode(expressionBase, expression);

            return EvaluateInstanceMemberNode(expression);
        }

        private DataType EvaluateStaticMemberNode(Token expressionBase, MemberNode expression)
        {
            var enumtype = GetEnum(expressionBase.Value, expressionBase.Position);
            if (enumtype is null)
                return ContextType;

            var result = DataType.Enum(enumtype);

            FunctionBuilder.EmitLoadConstantValue(GetEnumMemberValue(enumtype, expression.Member.Value, expression.Member.Position), result);

            return result;
        }

        private long GetEnumMemberValue(EnumStatement enumtype, string name, ModulePosition position)
        {
            foreach (var member in enumtype.Body)
                if (member.Name == name)
                    return GetEnumMemberValue(member);
            
            Tower.Report(position, $"Enum '{enumtype}' does not contain a definition for '{name}'");
            return -1;
        }

        private static long GetEnumMemberValue(EnumMemberNode member)
        {
            var result = long.Parse(member.Value.Value);
            if (member.IsNegative)
                result = -result;

            return result;
        }

        private EnumStatement GetEnum(string name, ModulePosition position)
        {
            return Tower.Symbols.GetSymbol<EnumStatement>(name, position, "enum neither a variable");
        }

        private DataType EvaluateInstanceMemberNode(MemberNode expression)
        {
            var baseType = EvaluateExpression(expression.Base);

            if (!baseType.SolvedType.IsNewOperatorAllocable())
                return EvaluateNonStructDotAccessOperator(baseType, expression);

            var structure = baseType.SolvedType.GetStruct();

            var type = GetFieldType(expression.Member.Value, structure, expression.Member.Position, out var index);
            LoadField(expression, structure, type, index);

            return type ?? ContextType;
        }

        private DataType EvaluateNonStructDotAccessOperator(DataType baseType, MemberNode expression)
        {
            if (baseType.SolvedType.IsStringOrArray())
                return EvaluateArrayDotAccessOperator(expression.Member, baseType);
            if (baseType.SolvedType.IsPointer())
                return EvaluatePointerDotAccessOperator(baseType, expression);

            return NotAccessibleViaDotOperator(baseType, expression.Base.Position);
        }

        private DataType NotAccessibleViaDotOperator(DataType type, ModulePosition position)
        {
            Tower.Report(position, $"Type '{type}' is not accessible via operator '.'");
            return ContextType;
        }

        private DataType EvaluatePointerDotAccessOperator(DataType baseType, MemberNode expression)
        {
            FunctionBuilder.EmitLoadValueFromPointer((baseType.SolvedType.GetBaseElementType()));

            var pointerBaseType = baseType.SolvedType.GetBaseElementType();

            if (!pointerBaseType.SolvedType.IsStruct())
                return NotAccessibleViaDotOperator(pointerBaseType, expression.Base.Position);

            var structure = pointerBaseType.SolvedType.GetStruct();
            var fieldType = GetFieldType(expression.Member.Value, structure, expression.Member.Position, out var index);
            FunctionBuilder.EmitLoadField(index, (fieldType));

            return fieldType;
        }

        private DataType EvaluateArrayDotAccessOperator(Token member, DataType type)
        {
            return member.Value switch
            {
                "len" => EvaluateLenFieldArray(),
                _ => error()
            };

            [DoesNotReturn]
            DataType error()
            {
                Tower.Report(member.Position, $"Unknown field '{member.Value}' for builtin type '{type}'");
                return ContextType;
            }
        }

        private DataType EvaluateLenFieldArray()
        {
            var type = ContextTypeIFIntOr(DataType.UInt64);

            FunctionBuilder.EmitLoadField(0, DataType.UInt64);
            IntCastIfNeeded(type);

            return type;
        }
        
        private void IntCastIfNeeded(DataType type)
        {
            if (type.SolvedType.Kind is not TypeKind.UInt64)
                FunctionBuilder.EmitCastIntToInt((type));
        }

        private DataType ContextTypeIFIntOr(DataType type)
        {
            return ContextType.SolvedType.IsInt() ? ContextType : type;
        }

        private void LoadField(MemberNode expression, TypeStatement structure, DataType type, int index)
        {
            if (type is null)
            {
                Tower.Report(
                    expression.Member.Position,
                    $"Type '{structure.Name}' does not contain a definition for '{expression.Member.Value}'");
                return;
            }
            
            FunctionBuilder.EmitLoadField(index, (type));
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
                var message =
                    expression.IsAuto ?
                        "Type notation needed" :
                        $"Unable to allocate type '{type}' via operator 'new'";

                Tower.Report(expression.Position, message);
                return DataType.Solved(type);
            }

            return EvaluateStruct(expression, DataType.Solved(type));
        }

        private DataType EvaluateStruct(TypeAllocationNode expression, DataType type)
        {
            var structure = type.SolvedType.GetStruct();
            var assignedFields = new List<string>();

            ReportStaticTypeAllocationIfNeeded(expression, structure);

            FunctionBuilder.EmitLoadZeroinitializedStruct((type));
            EvaluateTypeInitialization(expression, structure, assignedFields);
            // FunctionBuilder.EmitLoadValueFromPointer();

            return DataType.Solved(SolvedType.Struct(structure));
        }

        private void ReportStaticTypeAllocationIfNeeded(TypeAllocationNode expression, TypeStatement structure)
        {
            if (GetStructByteSize(structure) == 0)
                Tower.Report(expression.Position, "Unable to allocate a static type");
        }

        private long GetTypeByteSize(DataType type, ModulePosition position = default)
        {
            return type.SolvedType.Kind switch
            {
                TypeKind.Pointer => PointerSize(),
                TypeKind.Int32
                or TypeKind.UInt32 => 4,
                TypeKind.Int64
                or TypeKind.UInt64 => 8,
                TypeKind.Float32 => 4,
                TypeKind.Float64 => 8,
                TypeKind.Float128 => 16,
                TypeKind.Int8
                or TypeKind.Bool
                or TypeKind.UInt8
                or TypeKind.Char => 1,
                TypeKind.Int16
                or TypeKind.UInt16 => 2,
                TypeKind.Array
                or TypeKind.String => PointerSize() + 8,
                TypeKind.CustomType => GetStructByteSize(type.SolvedType.GetStruct(), position),
                TypeKind.GenericDefinedType => throw new NotImplementedException(),
                TypeKind.Void
                or TypeKind.Undefined
                or TypeKind.Auto => 0,
                TypeKind.Option => GetTypeByteSize(type.SolvedType.GetBaseElementType()) + 1
            };
        }

        private long GetStructByteSize(TypeStatement type, ModulePosition position = default)
        {
            long result = 0;
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
            return 8;
        }

        private void EvaluateTypeInitialization(TypeAllocationNode expression, TypeStatement structure, List<string> assignedFields)
        {
            for (var i = 0; i < expression.Body.Count; i++)
                EmitFieldInitialization(expression.Body[i], structure, assignedFields);

            if (structure.BodyFields.Count != assignedFields.Count)
                InitializeUninitializedFieldsWithDefaultValues(structure, expression.Position, assignedFields);
        }

        private void InitializeUninitializedFieldsWithDefaultValues(TypeStatement structure, ModulePosition position, List<string> assignedFields)
        {
            for (int i = 0; i < structure.BodyFields.Count; i++)
            {
                var field = structure.BodyFields[i];
                if (assignedFields.Contains(field.Name)) continue;
                
                EvaluateExpression(GetDefaultValueOf(field.Type, position));
                FunctionBuilder.EmitStoreField(i, (field.Type));
            }
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

            FixAndCheckTypes(fieldType, EvaluateExpression(field.Body), field.Body.Position);
            FunctionBuilder.EmitStoreField(fieldIndex, (fieldType));

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
                Tower.Report(LeftValueChecker.Position, "Constant allocation cannot be referred or assigned");

            FunctionBuilder.EmitLoadLocal(allocation.StackIndex, (allocation.Type));
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

        private DataType EvaluateConstant(Token constant)
        {
            DataType result;
            object value;

            switch (constant.Kind)
            {
                case TokenKind.ConstantDigit:
                    result = CoercedOr(DataType.Int32);
                    value = long.Parse(constant.Value);
                    break;
                case TokenKind.ConstantBoolean:
                    result = DataType.Bool;
                    value = bool.Parse(constant.Value);
                    break;
                case TokenKind.ConstantChar:
                    result = DataType.Char;
                    value = (long)constant.Value.First();
                    break;
                case TokenKind.ConstantString:
                    EmitLoadConstantString(constant.Value);
                    return DataType.String;
                default:
                    ToImplement<object>(constant.Kind.ToString(), nameof(EvaluateConstant));
                    result = null;
                    value = null;
                    break;
            }

            FunctionBuilder.EmitLoadConstantValue(value, (result));

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

            FunctionBuilder.DeclareAllocation((MIRAllocationAttribute)Convert.ToInt32(isconst), (type));

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
            FunctionBuilder = new(irFunctionName, (func.ReturnType), GetParameterTypes(func.ParameterList));
            CurrentFunction = func;
            CurrentScope = new(func.ReturnType, true, new());

            SetEntryBlock();

            ContextTypes.Push(func.ReturnType);
            AllocateParameters(func.ParameterList);
            GenerateBlock(func.Body);
            EmitOptionalReturn();
            ContextTypes.Pop();

            Module.DefineFunction(FunctionBuilder.Build());
        }

        public void EmitOptionalReturn()
        {
            if (FunctionBuilder.EmittedExplicitReturn())
                return;

            if (!CurrentFunction.ReturnType.SolvedType.IsVoid())
                EvaluateExpression(GetDefaultValueOf(CurrentFunction.ReturnType, CurrentFunction.Position));

            FunctionBuilder.EmitReturn((CurrentFunction.ReturnType));
        }

        private void GenerateFunctionPrototype(FunctionStatement func)
        {
            Module.DefineFunctionPrototype(func.Name, (func.ReturnType), GetParameterTypes(func.ParameterList));
        }

        private void SetEntryBlock()
        {
            FunctionBuilder.SwitchBlock(CreateBlock("entry"));
        }

        private void AllocateParameters(ParameterNode[] parameters)
        {
            foreach (var parameter in parameters)
                DeclareVirtualMemorySymbol(parameter.Name, parameter.Type, parameter.Position, true);
        }

        private void GenerateStruct(TypeStatement type)
        {
            CurrentType = type;

            foreach (var method in type.BodyMethods)
                GenerateFunction(method, $"{type.Name}.{method.Name}");

            CurrentType = null;
            Module.DefineStruct(LowerStruct(type));
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
                var structure = SearchForStructureInsideType(field.Type.SolvedType);
                if (structure is null) continue;

                if (illegalTypes.Contains(structure.Name))
                    Tower.Throw(field.Type.Position, $"Recursion in type, use '*{field.Type}' instead");

                CheckRecursiveType(structure, illegalTypes);
            }
        }

        private TypeStatement SearchForStructureInsideType(SolvedType type)
        {
            if (type.Base is null
                || type.IsPointer()
                || type.IsArray())
                return null;

            return
                type.IsStruct() ?
                    type.GetStruct() :
                    SearchForStructureInsideType(type.GetBaseElementType().SolvedType);
        }

        private void CheckStructureLayoutRecursion(TypeStatement type)
        {
            var errorsCount = Tower.Diagnostic.Count;

            CheckRecursiveType(type, new());

            Tower.CheckDiagnostic(errorsCount);
        }

        private void WalkThroughDeclarations()
        {
            FunctionStatement entrypoint = null;
            foreach (var symbol in Tower.Symbols.GetCache())
                RecognizeSymbol(symbol.Value, ref entrypoint);

            if (IsMainUnit())
                CheckEntryPoint(entrypoint);
        }

        private bool IsMainUnit()
        {
            return Tower.Unit.IsMainUnit;
        }

        private void CheckEntryPoint(FunctionStatement entryPoint)
        {
            if (entryPoint is null)
            {
                ReportMissingEntryPoint();
                return;
            }

            if (entryPoint.ParameterList.Length > 0)
                error("Entrypoint cannot have parameters");
            if (entryPoint.Generics.Count > 0)
                error("Entrypoint cannot have generic parameters");
            if (entryPoint.ReturnType.UnsolvedType.Kind is not TypeKind.Void)
                error("Entrypoint cannot return a value");
            if (entryPoint.Modifier is TokenKind.KeyPub)
                error("Entrypoint cannot have a public modifier");
            if (entryPoint.IsPrototype)
                error("Entrypoint cannot be declared as prototype");

            void error(string error)
            {
                Tower.Report(entryPoint.Position, error);
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
                case EnumStatement:
                    break;
                case TypeStatement structSymbol:
                    CheckStructureLayoutRecursion(structSymbol);
                    GenerateStruct(structSymbol);
                    break;
                case FunctionStatement funcSymbol:
                    if (funcSymbol.IsPrototype)
                        GenerateFunctionPrototype(funcSymbol);
                    else
                        SetEntryPointAndGenerateFunction(funcSymbol, ref entrypoint);
                    break;
                default:
                    ToImplement<object>(value.ToString(), nameof(RecognizeSymbol));
                    break;
            }
        }

        private FunctionStatement SetEntryPointAndGenerateFunction(FunctionStatement funcSymbol, ref FunctionStatement entrypoint)
        {
            if (funcSymbol.Name is EntryPointName)
                entrypoint = funcSymbol;

            GenerateFunction(funcSymbol, funcSymbol.Name);
            return entrypoint;
        }
    }
}