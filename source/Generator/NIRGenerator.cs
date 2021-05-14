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
                    if (!EvaluateNodeCall(statement).SolvedType.IsVoid())
                        FunctionBuilder.EmitPop();
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

                FunctionBuilder.EmitReturn(CurrentFunction.ReturnType);
            }
            else
            {
                if (CurrentScope.HiddenAllocationBuffer is null)
                    ContextTypesPushUndefined();
                else
                    ContextTypes.Push(CurrentScope.HiddenAllocationBuffer.Type);

                var allocation = TryAllocateHiddenBuffer(EvaluateExpression(expression));
                FixAndCheckTypes(allocation.Type, allocation.Type, expression.Position);

                FunctionBuilder.EmitStoreLocal(allocation.StackIndex, allocation.Type);
            }
        }

        private AllocationData TryAllocateHiddenBuffer(DataType type)
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

            CleanLeftValueChecker();
            ContextTypes.Pop();

            var instruction = FunctionBuilder.PopLastInstruction();

            if (!IsConvertibleToLeftExpressionInstruction(instruction.Kind))
                Tower.Report(statement.Name.Position, $"Expression in left side of assignment");

            ContextTypes.Push(variable);

            var expressiontype = EvaluateExpression(statement.Body);

            FixAndCheckTypes(variable, expressiontype, statement.Body.Position);

            ContextTypes.Pop();

            instruction.Kind = GetLeftExpressionInstruction(instruction.Kind);
            FunctionBuilder.EmitInstruction(instruction);
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
            ContextTypes.Push(statement.Type);

            if (!statement.IsAssigned)
            {
                if (statement.IsConst)
                    Tower.Report(statement.Position, "A constant declaration requires a body");

                if (statement.Type.SolvedType.Kind == TypeKind.Auto)
                    Tower.Report(statement.Position, "Type notation needed");
            }

            var expressiontype = EvaluateExpression(!statement.IsAssigned ? GetDefaultValueOf(statement.Type) : statement.Body);

            FixAndCheckTypes(statement.Type, expressiontype, statement.Body.Position);
            var allocation = DeclareVirtualMemorySymbol(statement.Name, statement.Type, statement.Position, statement.IsConst);

            FunctionBuilder.EmitStoreLocal(allocation.StackIndex, allocation.Type);

            ContextTypes.Pop();
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
                    type = EvaluateNodeTypeAllocation(expression);
                    break;
                case MemberNode expression:
                    type = EvaluateMemberNode(expression);
                    break;
                case BinaryExpressionNode expression:
                    type = EvaluateNodeBinaryExpression(expression);
                    break;
                case BlockNode expression:
                    type = EvaluateNodeBlock(expression);
                    break;
                case CallStatement expression:
                    type = EvaluateNodeCall(expression);
                    break;
                default:
                    CompilationTower.Todo($"implement {body} in NIRGenerator.EvaluateExpression");
                    break;
            }

            if (type.SolvedType.IsVoid())
                Tower.Report(body.Position, "Expected a non-void expression");

            return type;
        }

        private DataType EvaluateNodeCall(CallStatement expression)
        {
            var parameters = expression.Parameters;
            var funcSymbol = EvaluateFunctionName(expression.Name, ref parameters);
            expression.Parameters = parameters;

            if (funcSymbol is null)
                return DataType.Void;

            var func = funcSymbol.Func;
            EvaluateCallParameters(expression, func);

            return func.ReturnType;
        }

        private FuncSymbol EvaluateFunctionName(INode functioName, ref NodeBuilder parameters)
        {
            FuncSymbol funcSymbol;
            switch (functioName)
            {
                case Token name:
                    funcSymbol = SearchForFunction(name);
                    break;
                case MemberNode name:
                    funcSymbol = EvaluateBaseFunctionName(name, ref parameters);
                    break;
                default:
                    CompilationTower.Todo($"implement {functioName} in NIRGenerator.EvaluateNodeCall");
                    throw new();
            }

            return funcSymbol;
        }

        private FuncSymbol EvaluateBaseFunctionName(MemberNode name, ref NodeBuilder parameters)
        {
            return
                name.Base is Token baseToken ?
                    EvaluateTokenBaseFunctionName(name, parameters, baseToken) :
                    EvaluateExpressionBaseFunctionName(name, parameters);
        }

        private FuncSymbol EvaluateExpressionBaseFunctionName(MemberNode name, NodeBuilder parameters)
        {
            parameters.Prepend(name.Base);

            var type = GetExpressionType(name.Base);

            if (!type.SolvedType.IsStruct())
                return ReportPrimitiveCannotHaveMethods(name.Base.Position);

            return SearchForMethod(name.Member.Value, type.SolvedType.GetStruct().Type);
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

        private FuncSymbol EvaluateTokenBaseFunctionName(MemberNode name, NodeBuilder parameters, Token baseToken)
        {
            if (baseToken.Kind != TokenKind.Identifier)
                return ReportPrimitiveCannotHaveMethods(baseToken.Position);

            string typeName;
            if (GetLocalVariable(baseToken.Value, out var allocation))
            {
                if (!allocation.Type.SolvedType.IsStruct())
                    return ReportPrimitiveCannotHaveMethods(baseToken.Position);

                parameters.Prepend(baseToken);
                typeName = allocation.Type.SolvedType.GetStruct().Type.Name;
            }
            else
                typeName = baseToken.Value;

            var type = Tower.Symbols.GetSymbol<StructSymbol>(typeName, baseToken.Position, "type");

            return type is null ? null : SearchForMethod(name.Member.Value, type.Type);
        }

        private FuncSymbol ReportPrimitiveCannotHaveMethods(ModulePosition position)
        {
            Tower.Report(position, "Primitive types don't support methods");
            return null;
        }

        private FuncSymbol SearchForMethod(string value, TypeStatement type)
        {
            foreach (var method in type.BodyMethods)
                if (value == method.Name)
                    return new(method);

            Tower.Report(type.Position, $"No method '{value}' declared in type '{type.Name}'");
            return null;
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
            {
                Tower.Report(parameter.Position, "Unexpected extra function parameter");
                EvaluateParameter(DataType.Auto, parameter);
            }
            else
            {
                var prototypeParameterType = func.ParameterList.Parameters[i].Type;
                var expressionType = EvaluateParameter(prototypeParameterType, parameter);
                FixAndCheckTypes(prototypeParameterType, expressionType, parameter.Position);
            }
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

        private FuncSymbol SearchForFunction(Token name)
        {
            return Tower.Symbols.GetSymbol<FuncSymbol>(name.Value, name.Position, "function");
        }

        private DataType EvaluateNodeBlock(BlockNode expression)
        {
            var oldScope = CurrentScope;
            CurrentScope = new(null, false, CurrentScope.VirtualMemory);

            GenerateBlock(expression);

            var hiddentAllocation = CurrentScope.HiddenAllocationBuffer;
            CurrentScope = oldScope;
            return hiddentAllocation is not null ? hiddentAllocation.Type : DataType.Void;
        }

        private DataType EvaluateNodeBinaryExpression(BinaryExpressionNode expression)
        {
            var leftisconstant = IsConstantInt(expression.Left);
            var rightisconstant = IsConstantInt(expression.Right);
            DataType type;

            // make it better
            if (leftisconstant & rightisconstant)
            {
                var constant = long.Parse(FoldConstantIntoToken(expression).Value);

                FunctionBuilder.EmitLoadConstantValue(constant, type = CoercedOr(DataType.Int32));
            }
            else
            {
                if (leftisconstant)
                {
                    var index = FunctionBuilder.CurrentIndex();
                    var left = FoldConstantIntoToken(expression.Left);

                    FunctionBuilder.EmitLoadConstantValue(
                        long.Parse(left.Value),
                        type = EvaluateExpression(expression.Right));

                    FunctionBuilder.MoveLastInstructionTo(index);
                }
                else if (rightisconstant)
                {
                    var right = FoldConstantIntoToken(expression.Right);

                    FunctionBuilder.EmitLoadConstantValue(
                        long.Parse(right.Value),
                        type = EvaluateExpression(expression.Left));
                }
                else
                {
                    type = EvaluateExpression(expression.Left);
                    FixAndCheckTypes(type, EvaluateExpression(expression.Right), expression.Right.Position);
                    // allow user defined operators
                }

                EmitOperation(expression.Operator, type);
            }

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

        private DataType EvaluateMemberNode(MemberNode expression)
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
                FunctionBuilder.EmitLoadField(index, type);

            return type ?? ContextType;
        }

        private DataType EvaluateNodeTypeAllocation(TypeAllocationNode expression)
        {
            SolvedType type;
            if (expression.Name is not null)
                type = expression.Name.SolvedType;
            else if (ContextTypeIsAmbiguousOrGet(expression.Position, out type))
                return DataType.Void;

            if (!type.IsNewOperatorAllocable())
            {
                Tower.Report(expression.Position, $"Unable to allocate type '{type}' via operator 'new'");
                return DataType.Solved(type);
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
                FunctionBuilder.EmitStoreField(fieldindex, fieldtype);

                ContextTypes.Pop();
            }

            return DataType.Solved(SolvedType.Struct(result));
        }

        private bool ContextTypeIsAmbiguousOrGet(ModulePosition position, out SolvedType type)
        {
            type = ContextType.SolvedType;
            var isambiguous = type.IsAuto();
            if (isambiguous)
                Tower.Report(position, "Cannot infer ambiguous type");

            return isambiguous;
        }

        private static DataType GetFieldType(string name, List<FieldNode> body, out int i)
        {
            for (i = 0; i < body.Count; i++)
            {
                var field = body[i];
                if (name == field.Name)
                    return field.Type;
            }

            return null;
        }

        private DataType EvaluateIdentifier(string value, ModulePosition position)
        {
            if (!GetLocalVariable(value, out var allocation))
            {
                allocation = new AllocationData(0, ContextType, false);
                Tower.Report(position, $"Variable '{value}' is not declared");
            }
            else
            {
                if (allocation.IsConst && LeftValueChecker.IsLeftValue)
                    Tower.Report(LeftValueChecker.Position, "Constant allocation in left side of assignement");

                FunctionBuilder.EmitLoadLocal(allocation.StackIndex, allocation.Type);
            }

            return allocation.Type;
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
                    CompilationTower.Todo($"implement {expression.Kind} in NIRGenerator.EvaluateConstant");
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
            var localindex = GetLocalIndex();
            var allocation = new AllocationData(localindex, type, isconst);
            if (!CurrentScope.VirtualMemory.TryAdd(name, allocation))
                Tower.Report(position, $"Variable '{name}' is already declared");

            FunctionBuilder.DeclareAllocation(type);

            return allocation;
        }

        private int GetLocalIndex()
        {
            return CurrentScope.VirtualMemory.Count;
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

        private bool IsMethod()
        {
            return CurrentType is not null;
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
                        (fieldtype.Kind == TypeKind.Pointer ?
                            ((DataType)fieldtype.Base).SolvedType : fieldtype
                        ).GetStruct().Type;

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
                case StructSymbol structsymbol:
                    CheckRecursiveType(structsymbol.Type, new());
                    GenerateStruct(structsymbol.Type);
                    break;
                case FuncSymbol funcsymbol:
                    GenerateFunction(funcsymbol.Func, funcsymbol.Func.Name);
                    break;
                default:
                    CompilationTower.Todo($"implement {value} in NIRGenerator.RecognizeSymbol");
                    break;
            }
        }
    }
}
