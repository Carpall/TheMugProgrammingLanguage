using Nylon.Compilation;
using Nylon.Models.Generator.IR;
using Nylon.Models.Generator.IR.Builder;
using Nylon.Models.Lexer;
using Nylon.Models.Parser;
using Nylon.Models.Parser.AST;
using Nylon.Models.Parser.AST.Statements;
using Nylon.Symbols;
using Nylon.TypeSystem;
using System;
using System.Collections.Generic;

namespace Nylon.Models.Generator
{
    public class NIRGenerator : CompilerComponent
    {
        public NIRModuleBuilder Module { get; } = new();

        private NIRFunctionBuilder FunctionBuilder { get; set; }
        private FunctionStatement CurrentFunction { get; set; }
        private TypeStatement CurrentType { get; set; }
        private Stack<DataType> ContextTypes { get; set; } = new();
        
        private DataType ContextType => ContextTypes.Peek();
        private Scope CurrentScope = default;
        private (bool IsLeftValue, ModulePosition Position) LeftValueChecker;

        public NIRGenerator(CompilationTower tower) : base(tower)
        {
        }

        public NIR Generate()
        {
            WalkDeclarations();

            Tower.CheckDiagnostic();
            return Module.Build();
        }

        private static DataType[] GetParameterTypes(ParameterListNode parameters)
        {
            var result = new DataType[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                result[i] = parameters.Parameters[i].Type;

            return result;
        }

        private void GenerateBlock(BlockNode block)
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
                default:
                    if (!isLastNodeOfBlock)
                        Tower.Report(opaque.Position, "Expression evaluable only when is last of a block");
                    
                    EvaluateExpressionInHiddenBuffer(opaque);
                    break;
            }
        }

        private void GenerateConditionalStatement(ConditionalStatement statement, bool isLastOfBlock)
        {
            var condition = EvaluateConditionExpression(statement);
            StoreInHiddenBufferIfNeeded(condition, isLastOfBlock, statement.Position);
        }

        private DataType EvaluateConditionExpression(ConditionalStatement expression)
        {
            ContextTypes.Push(DataType.Bool);
            var condition = EvaluateExpression(expression.Expression);
            ContextTypes.Pop();

            FixAndCheckTypes(DataType.Bool, condition, expression.Expression.Position);

            var label = FunctionBuilder.EmitJumpFalse("if_then");

            var conditionblock = EvaluateNodeBlockInExpression(expression.Body);

            return conditionblock;
        }

        private AllocationData GetHiddenBufferTypeOrVoid()
        {
            return
                CurrentScope.HiddenAllocationBuffer is not null ?
                    CurrentScope.HiddenAllocationBuffer :
                    CreateVoidAllocation();
        }

        private static AllocationData CreateVoidAllocation()
        {
            return new(0, DataType.Void, false);
        }

        private void StoreInHiddenBufferIfNeeded(DataType type, bool isLastOfBlock, ModulePosition position)
        {
            if (!type.SolvedType.IsVoid())
            {
                if (!isLastOfBlock)
                    FunctionBuilder.EmitPop();
                else
                {
                    SetupOrStoreInHiddenBuffer(type, position);
                    if (CurrentScope.IsInFunctionBlock)
                        FunctionBuilder.EmitAutoReturn();
                }
            }
        }

        private void GenerateCallStatement(CallStatement statement, bool isLastOfBlock)
        {
            var call = EvaluateNodeCall(statement);
            StoreInHiddenBufferIfNeeded(call, isLastOfBlock, statement.Position);
        }

        private void SetupOrStoreInHiddenBuffer(DataType type, ModulePosition position)
        {
            var allocation = TryAllocateHiddenBuffer(type);
            FixAndCheckTypes(allocation.Type, allocation.Type, position);

            FunctionBuilder.EmitStoreLocal(allocation.StackIndex, allocation.Type);
        }

        private void EvaluateExpressionInHiddenBuffer(INode expression)
        {
            if (CurrentScope.IsInFunctionBlock)
                EvaluateHiddenBufferInFunctionBlock(expression);
            else
                EvaluateHiddenBufferInSubBlock(expression);
        }

        private void EvaluateHiddenBufferInSubBlock(INode expression)
        {
            if (CurrentScope.HiddenAllocationBuffer is null)
                ContextTypesPushUndefined();
            else
                ContextTypes.Push(CurrentScope.HiddenAllocationBuffer.Type);

            SetupOrStoreInHiddenBuffer(EvaluateExpression(expression), expression.Position);
        }

        private void EvaluateHiddenBufferInFunctionBlock(INode expression)
        {
            ContextTypes.Push(CurrentFunction.ReturnType);
            FixAndCheckTypes(CurrentFunction.ReturnType, EvaluateExpression(expression), expression.Position);

            FunctionBuilder.EmitReturn(CurrentFunction.ReturnType);
        }

        private AllocationData TryAllocateHiddenBuffer(DataType type)
        {
            if (CurrentScope.HiddenAllocationBuffer is null)
            {
                FunctionBuilder.DeclareAllocation(NIRAllocationAttribute.HiddenBuffer, type);
                CurrentScope.HiddenAllocationBuffer = new(FunctionBuilder.GetAllocationNumber(), type, false);
            }

            return CurrentScope.HiddenAllocationBuffer;
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

        private NIRValue EvaluateLeftSideOfAssignment(AssignmentStatement statement, DataType variable)
        {
            CleanLeftValueChecker();
            ContextTypes.Pop();

            var instruction = FunctionBuilder.PopLastInstruction();

            if (!IsConvertibleToLeftExpressionInstruction(instruction.Kind))
                Tower.Report(statement.Name.Position, $"Expression in left side of assignment");

            ContextTypes.Push(variable);
            return instruction;
        }

        private static bool IsConvertibleToLeftExpressionInstruction(NIRValueKind kind)
        {
            return kind == NIRValueKind.LoadLocal || kind == NIRValueKind.LoadField;
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

        private static NIRValueKind GetLeftExpressionInstruction(NIRValueKind kind)
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

            FunctionBuilder.EmitStoreLocal(allocation.StackIndex, allocation.Type);

            ContextTypes.Pop();
        }

        private void CheckVariable(VariableStatement statement)
        {
            if (!statement.IsAssigned)
            {
                if (statement.IsConst)
                    Tower.Report(statement.Position, "A constant declaration requires a body");

                if (statement.Type.SolvedType.Kind == TypeKind.Auto)
                    Tower.Report(statement.Position, "Type notation needed");
            }
        }

        private static INode GetDefaultValueOf(DataType type)
        {
            return new BadNode();
        }

        private void FixAndCheckTypes(DataType expected, DataType gottype, ModulePosition position)
        {
            FixAuto(expected, gottype);

            var rightsolved = expected.SolvedType;
            var leftsolved = gottype.SolvedType;

            if (rightsolved.Kind != leftsolved.Kind || rightsolved.Base is not null && !rightsolved.Base.Equals(leftsolved.Base))
                Tower.Report(position, $"Type mismatch: expected type '{expected}', but got '{gottype}'");
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
                Token expression => expression.Kind == TokenKind.Identifier ?
                    EvaluateIdentifier(expression.Value, expression.Position) :
                    EvaluateConstant(expression),
                TypeAllocationNode expression => EvaluateNodeTypeAllocation(expression),
                MemberNode expression => EvaluateMemberNode(expression),
                BinaryExpressionNode expression => EvaluateNodeBinaryExpression(expression),
                BlockNode expression => EvaluateNodeBlockInExpression(expression),
                CallStatement expression => EvaluateNodeCall(expression),
                _ => ToImplement<DataType>(body.ToString(), "EvaluateExpression"),
            };

            if (type.SolvedType.IsVoid())
                Tower.Report(body.Position, "Expected a non-void expression");

            return type;
        }

        private static T ToImplement<T>(string value, string function)
        {
            CompilationTower.Todo($"implement {value} in NIRGenerator.{function}");
            return default;
        }

        private DataType EvaluateNodeCall(CallStatement expression)
        {
            var parameters = expression.Parameters;
            var func = EvaluateFunctionName(expression.Name, ref parameters);
            expression.Parameters = parameters;

            if (func is null)
                return DataType.Void;

            EvaluateCallParameters(expression, func);

            return func.ReturnType;
        }

        private FunctionStatement EvaluateFunctionName(INode functionName, ref NodeBuilder parameters)
        {
            var funcSymbol = functionName switch
            {
                Token name => SearchForFunctionOrStaticMethod(name),
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
            var oldFunctionBuilder = new NIRFunctionBuilder(FunctionBuilder);

            ContextTypesPushUndefined();
            var type = EvaluateExpression(expression);
            ContextTypes.Pop();

            FunctionBuilder = oldFunctionBuilder;
            return type;
        }

        private FunctionStatement EvaluateTokenBaseFunctionName(MemberNode name, NodeBuilder parameters, Token baseToken)
        {
            if (baseToken.Kind != TokenKind.Identifier)
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

            var type = Tower.Symbols.GetSymbol<TypeStatement>(typeName, baseToken.Position, "type");

            return type is null ? null : SearchForMethod(name.Member.Value, type, name.Member.Position);
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

            for (int i = 0; i < expression.Parameters.Count; i++)
                CheckAndEvaluateParameter(func, i, expression.Parameters[i]);

            FunctionBuilder.EmitCall(func.Name, func.ReturnType);
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

        private FunctionStatement SearchForFunctionOrStaticMethod(Token name)
        {
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

        private AllocationData GenerateNodeBlockInExpression(BlockNode expression)
        {
            var oldScope = CurrentScope;
            CurrentScope = new(null, false, CurrentScope.VirtualMemory);

            GenerateBlock(expression);

            var allocation = GetHiddenBufferTypeOrVoid();
            CurrentScope = oldScope;
            return allocation;
        }

        private DataType EvaluateNodeBlockInExpression(BlockNode expression)
        {
            var allocation = GenerateNodeBlockInExpression(expression);
            if (!allocation.Type.SolvedType.IsVoid())
                FunctionBuilder.EmitLoadLocal(allocation.StackIndex, allocation.Type);

            return allocation.Type;
        }

        private DataType EvaluateNodeBinaryExpression(BinaryExpressionNode expression)
        {
            var leftisconstant = IsConstantInt(expression.Left);
            var rightisconstant = IsConstantInt(expression.Right);
            DataType type;

            // make it better
            if (leftisconstant & rightisconstant)
                type = EvaluateBinaryConstant(expression);
            else
            {
                type = EvaluateSemiConstantBinaryOrNonConstant(expression, leftisconstant, rightisconstant);

                EmitOperation(expression.Operator, type);
            }

            return type;
        }

        private DataType EvaluateSemiConstantBinaryOrNonConstant(
            BinaryExpressionNode expression,
            bool leftisconstant,
            bool rightisconstant)
        {
            return
                leftisconstant ?
                    EvaluateLeftIsConstant(expression) :
                    rightisconstant ?
                        EvaluateRightIsConstant(expression) :
                        EvaluateNoConstants(expression);
        }

        private DataType EvaluateBinaryConstant(BinaryExpressionNode expression)
        {
            var constant = long.Parse(FoldConstantIntoToken(expression).Value);

            var type = CoercedOr(DataType.Int32);
            FunctionBuilder.EmitLoadConstantValue(constant, type);
            return type;
        }

        private DataType EvaluateNoConstants(BinaryExpressionNode expression)
        {
            DataType type = EvaluateExpression(expression.Left);
            FixAndCheckTypes(type, EvaluateExpression(expression.Right), expression.Right.Position);
            // allow user defined operators
            return type;
        }

        private DataType EvaluateRightIsConstant(BinaryExpressionNode expression)
        {
            var right = FoldConstantIntoToken(expression.Right);

            var type = EvaluateExpression(expression.Left);
            var constantRight = ulong.Parse(right.Value);
            ReportWhenDividingByZero(constantRight, expression.Operator == TokenKind.Slash, expression.Position);

            FunctionBuilder.EmitLoadConstantValue(constantRight, type);

            return type;
        }

        private bool ReportWhenDividingByZero(ulong right, bool isDividing, ModulePosition position)
        {
            var dividingByZero = right == 0 && isDividing;

            if (dividingByZero)
                Tower.Report(position, "Dividing by '0' at compile time");

            return dividingByZero;
        }

        private DataType EvaluateLeftIsConstant(BinaryExpressionNode expression)
        {
            var index = FunctionBuilder.CurrentIndex();
            var left = FoldConstantIntoToken(expression.Left);

            ContextTypesPushUndefined();

            var type = EvaluateExpression(expression.Right);
            FunctionBuilder.EmitLoadConstantValue(long.Parse(left.Value), type);

            ContextTypes.Pop();

            FunctionBuilder.MoveLastInstructionTo(index);
            return type;
        }

        private void EmitOperation(TokenKind op, DataType type)
        {
            FunctionBuilder.EmitInstruction(op switch
            {
                TokenKind.Plus => NIRValueKind.Add,
                TokenKind.Minus => NIRValueKind.Sub,
                TokenKind.Star => NIRValueKind.Mul,
                TokenKind.Slash => NIRValueKind.Div,
                _ => throw new()
            }, type);
        }

        private Token FoldConstantIntoToken(INode opaque)
        {
            // make it better when introduce floating points
            return opaque switch
            {
                BadNode => Token.NewInfo(TokenKind.ConstantDigit, "1"),
                Token expression => expression,
                BinaryExpressionNode expression => new Token(
                    TokenKind.ConstantDigit,
                    FoldConstants(
                        ulong.Parse(FoldConstantIntoToken(expression.Left).Value),
                        ulong.Parse(FoldConstantIntoToken(expression.Right).Value),
                        expression.Operator,
                        opaque.Position).ToString(),
                    expression.Position,
                    false),
                _ => Token.NewInfo(TokenKind.Bad, "")
            };
        }

        private ulong FoldConstants(ulong left, ulong right, TokenKind op, ModulePosition position)
        {
            return op switch
            {
                TokenKind.Plus => left + right,
                TokenKind.Minus => left - right,
                TokenKind.Star => left * right,
                TokenKind.Slash => EvaluateDivideConstants(left, right, position),
            };
        }

        private ulong EvaluateDivideConstants(ulong left, ulong right, ModulePosition position)
        {
            return !ReportWhenDividingByZero(right, true, position) ? left / right : 0;
        }

        private static bool IsConstantInt(INode node)
        {
            return
                (node is BadNode) ||
                (node is Token token && token.Kind == TokenKind.ConstantDigit) ||
                (node is BinaryExpressionNode binary && IsConstantInt(binary.Left) && IsConstantInt(binary.Right));
        }

        private DataType EvaluateMemberNode(MemberNode expression)
        {
            var basetype = EvaluateExpression(expression.Base);
            if (!basetype.SolvedType.IsNewOperatorAllocable())
            {
                Tower.Report(expression.Base.Position, $"Type '{basetype}' is not accessible via operator '.'");
                return ContextType;
            }

            var structure = basetype.SolvedType.GetStruct();

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
                FunctionBuilder.EmitLoadField(index, type);
        }

        private void ExpectPublicFieldOrInternal(FieldNode field, TypeStatement type, ModulePosition position)
        {
            if (field.Modifier != TokenKind.KeyPub && !ProcessingMethodOf(type))
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

            return EvaluateStruct(expression, type);
        }

        private DataType EvaluateStruct(TypeAllocationNode expression, SolvedType type)
        {
            var structure = type.GetStruct();
            var assignedFields = new List<string>();

            FunctionBuilder.EmitLoadZeroinitializedStruct(expression.Name);

            EvaluateTypeInitialization(expression, structure, assignedFields);

            return DataType.Solved(SolvedType.Struct(structure));
        }

        private void EvaluateTypeInitialization(TypeAllocationNode expression, TypeStatement structure, List<string> assignedFields)
        {
            for (int i = 0; i < expression.Body.Count; i++)
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
            var fieldtype = GetFieldType(field.Name, structure, field.Position, out var fieldindex);

            if (fieldtype is null)
            {
                Tower.Report(field.Position, $"Type '{structure.Name}' does not contain a definition for '{field.Name}'");
                return;
            }

            EvaluateFieldAsignmentInInitialization(field, fieldtype, fieldindex);
        }

        private void EvaluateFieldAsignmentInInitialization(FieldAssignmentNode field, DataType fieldtype, int fieldindex)
        {
            ContextTypes.Push(fieldtype);

            FunctionBuilder.EmitDupplicate();
            FixAndCheckTypes(fieldtype, EvaluateExpression(field.Body), field.Position);
            FunctionBuilder.EmitStoreField(fieldindex, fieldtype);

            ContextTypes.Pop();
        }

        private bool ContextTypeIsAmbiguousOrGet(ModulePosition position, out SolvedType type)
        {
            type = ContextType.SolvedType;
            var isambiguous = type.IsAuto();
            if (isambiguous)
                Tower.Report(position, "Cannot infer ambiguous type");

            return isambiguous;
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

            FunctionBuilder.EmitLoadLocal(allocation.StackIndex, allocation.Type);
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
                    ToImplement<object>(expression.Kind.ToString(), "EvaluateConstant");
                    result = null;
                    value = new();
                    break;
            }

            FunctionBuilder.EmitLoadConstantValue(value, result);

            return result;
        }

        private DataType CoercedOr(DataType or)
        {
            var contexttype = ContextTypes.Peek();
            return contexttype.SolvedType.IsInt() ? contexttype : or;
        }

        private AllocationData DeclareVirtualMemorySymbol(string name, DataType type, ModulePosition position, bool isconst)
        {
            var localindex = GetAllocationsNumber();
            var allocation = new AllocationData(localindex, type, isconst);
            CheckForRedeclaration(name, position, allocation);

            FunctionBuilder.DeclareAllocation((NIRAllocationAttribute)Convert.ToInt32(isconst), type);

            return allocation;
        }

        private void CheckForRedeclaration(string name, ModulePosition position, AllocationData allocation)
        {
            if (!CurrentScope.VirtualMemory.TryAdd(name, allocation))
                Tower.Report(position, $"Variable '{name}' is already declared");
        }

        private int GetAllocationsNumber()
        {
            return FunctionBuilder.GetAllocationNumbers();
        }

        private void GenerateFunction(FunctionStatement func, string nirFunctionName)
        {
            FunctionBuilder = new NIRFunctionBuilder(nirFunctionName, func.ReturnType, GetParameterTypes(func.ParameterList));
            CurrentFunction = func;
            CurrentScope = new(null, true, new());

            AllocateParameters(func.ParameterList);
            GenerateBlock(func.Body);
            FunctionBuilder.EmitOptionalReturnVoid();

            Module.DefineFunction(FunctionBuilder.Build());
        }

        private void AllocateParameters(ParameterListNode parameters)
        {
            foreach (var parameter in parameters.Parameters)
                DeclareVirtualMemorySymbol(parameter.Name, parameter.Type, parameter.Position, false);
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

        private void CheckRecursiveType(TypeStatement type, List<string> illegaltypes)
        {
            illegaltypes.Add(type.Name);

            foreach (var field in type.BodyFields)
            {
                var fieldtype = field.Type.SolvedType;
                if (fieldtype.IsStruct() ||
                    (fieldtype.IsPointer() && fieldtype.GetBaseElementType().SolvedType.IsStruct()))
                {
                    var fieldstructtype =
                        (
                            fieldtype.Kind == TypeKind.Pointer ?
                                (fieldtype.Base as DataType).SolvedType :
                                fieldtype
                        ).GetStruct();

                    CheckIfThereIsARecursion(type, illegaltypes, field, fieldtype, fieldstructtype);
                }
            }
        }

        private void CheckIfThereIsARecursion(
            TypeStatement type,
            List<string> illegaltypes,
            FieldNode field,
            SolvedType fieldtype,
            TypeStatement fieldstructtype)
        {
            if (illegaltypes.Contains(fieldtype.ToString()))
            {
                Tower.Report(type.Position, "Recursive type");
                Tower.Report(field.Type.Position, $"Use '?{fieldtype}' instead");
            }
            else
                CheckRecursiveType(fieldstructtype, illegaltypes);
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
                case TypeStatement structsymbol:
                    CheckRecursiveType(structsymbol, new());
                    GenerateStruct(structsymbol);
                    break;
                case FunctionStatement funcsymbol:
                    GenerateFunction(funcsymbol, funcsymbol.Name);
                    break;
                default:
                    ToImplement<object>(value.ToString(), "RecognizeSymbol");
                    break;
            }
        }
    }
}
