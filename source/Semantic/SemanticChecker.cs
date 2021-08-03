using Mug.Compilation;
using Mug.Grammar;
using Mug.Syntax.AST;
using Mug.Typing;
using Mug.Typing.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Semantic
{
    public class SemanticChecker : CompilerComponent
    {
        private const string EntryPointName = "main";

        private NamespaceNode AST { get; set; }

        public List<MemoryDetail> Memory;

        private readonly Queue<Value> _staticParameters = new();

        private readonly Stack<IType> _contextTypes = new();

        private readonly List<string> _inProgressMembers = new();

        private IType ContextType => _contextTypes.Peek();

        private bool SeekType { get; set; }

        private FunctionType CurrentFunctionType { get; set; }

        private VariableNode CurrentVariable { get; set; }

        public SemanticChecker(CompilationInstance tower) : base(tower)
        {
        }

        public void SetAST(NamespaceNode ast)
        {
            AST = ast;
            Memory = new(ast.Members.Count);
        }

        private void PushContextType(IType type)
        {
            _contextTypes.Push(type);
        }

        private void PopContextType()
        {
            _contextTypes.Pop();
        }

        public NamespaceNode Check()
        {
            Reset();

            DoWithContextType(IType.BadType, () =>
            {
                DeclareAllBuiltinMembers();
                DeclareAllGlobalMembers();

                var main = GetMain();

                EvaluateVariable(main, false);

                ReportEntrypointWrongType(main);
            });

            return AST;
        }

        private VariableNode GetMain()
        {
            if (!SearchForInMemory(EntryPointName, out var entryPoint))
                CompilationInstance.Throw("Expected entrypoint");

            return (VariableNode)entryPoint.Value;
        }

        private void ReportEntrypointWrongType(VariableNode main)
        {
            if (main.NodeType is not FunctionType fnType
                || fnType.ParameterTypes.Length > 0
                || fnType.ReturnType is not VoidType)
                Tower.Report(main.Position, $"Entrypoint must be of type 'fn(): void'");
        }

        private void DoWithContextType(IType type, Action action)
        {
            PushContextType(type);
            action();
            PopContextType();
        }

        private T DoWithContextType<T>(IType type, Func<T> action)
        {
            PushContextType(type);
            var result = action();
            PopContextType();
            return result;
        }

        private void EvaluateVariable(VariableNode variable, bool isStatic)
        {
            if (isStatic)
            {
                evaluateVariableBody();
                return;
            }

            if (IsInProgress(variable.Name))
                return;

            PutInProgress(variable.Name);

            evaluateVariableBody();

            RemoveFromInProgress(variable.Name);

            void evaluateVariableBody() => EvaluateVariableBody(variable);
        }

        private void EvaluateVariableBody(VariableNode variable)
        {
            CurrentVariable = variable;
            DoWithMemoryConfiguration(() => TypeNode(variable, CheckExpression(variable.Body).Type));
        }

        private void RemoveFromInProgress(string name) => _inProgressMembers.Remove(name);

        private void PutInProgress(string name) => _inProgressMembers.Add(name);

        private bool IsInProgress(string name) => _inProgressMembers.Contains(name);

        private Value CheckExpression(INode body)
        {
            var result = body switch
            {
                FunctionNode function => CheckFunction(function),
                Token token => CheckToken(token),
                CallNode call => CheckCall(call),
            };

            ExpectExpressionWithType(body, result);

            return result;
        }

        private void ExpectExpressionWithType(INode body, Value result)
        {
            if (result.Type is VoidType)
                Tower.Report(body.Position, $"Expression of type 'void'");
        }

        private Value CheckToken(Token token) => token.Kind is TokenKind.Identifier ? CheckIdentifier(token) : CheckConstant(token);

        private Value CheckIdentifier(Token token)
        {
            var detail = GetFromMemory(token.Value, token.Position);
            if (detail.Value is Value value)
                return value;

            return new(detail.Type, detail.Value, detail.Value is VariableNode variable && variable.IsConst);
        }

        private Value CheckConstant(Token token)
        {
            var type = TokenKindToType(token);
            return new(type, ParseConstantFollowingType(token.Value, type), true);
        }

        private static object ParseConstantFollowingType(string value, IType type) => type switch
        {
            IntType => long.Parse(value)
        };

        private IType TokenKindToType(Token token) => token.Kind switch
        {
            TokenKind.ConstantDigit => ContextTypeIntOr(IType.Int(32, true))
        };

        private IntType ContextTypeIntOr(IntType intType) => ContextType is IntType t ? t : intType;

        private Value CheckFunction(FunctionNode function)
        {
            TypeNode(function, MakeFunctionType(function, out var returnType));

            if (!SeekType)
                CheckFunctionBody(function, returnType);

            return new(function.NodeType, function, true);
        }

        private void CheckFunctionBody(FunctionNode function, IType returnType)
        {
            CurrentFunctionType = (FunctionType)function.NodeType;
            DeclareParameters(function.ParameterList);
            CheckStatements(function.Body);
        }

        private void DoWithMemoryConfiguration(Action action)
        {
            var old = ConfigureMemory();
            action();
            RestoreMemory(old);
        }

        private void RestoreMemory(List<MemoryDetail> old)
        {
            Memory = old;
        }

        private List<MemoryDetail> ConfigureMemory()
        {
            var old = Memory;
            Memory = new(Memory);
            return old;
        }

        private void CheckStatements(BlockNode body)
        {
            foreach (var statement in body.Statements)
                CheckStatement(statement);
        }

        private void CheckStatement(INode statement)
        {
            switch (statement)
            {
                case CallNode call:
                    CheckCall(call);
                    break;
                case VariableNode variable:
                    CheckVariable(variable);
                    break;
                case ReturnNode ret:
                    CheckReturn(ret);
                    break;
            }
        }

        private void CheckReturn(ReturnNode ret)
        {
            DoWithContextType(CurrentFunctionType.ReturnType, () =>
            {
                var value = !ret.IsVoid() ? CheckExpression(ret.Body) : new(IType.Void, null, true);
                CheckTypes(CurrentFunctionType.ReturnType, value.Type, ret.Body.Position);
            });
        }

        private void CheckVariable(VariableNode variable)
        {
            variable.DeclarationIndex = Memory.Count;

            if (variable.IsConst)
            {
                DeclareLazyLocalConstantVariable(variable);
                return;
            }

            var explicitType = EvaluateType(variable.Type);
            var body = DoWithContextType(explicitType, () => CheckExpression(variable.Body));
            var type = RealTypeOr(explicitType, body.Type);

            CheckTypes(type, body.Type, variable.Body.Position);

            if (body.IsSpecial() & !variable.IsConst)
                Tower.Report(variable.Body.Position, $"Special value needs constant context");

            TypeNode(variable, type);
            DeclareLocalMember(variable.Name, variable.IsMutable, variable.NodeType, variable.Position);
        }

        private void DeclareLazyLocalConstantVariable(VariableNode variable)
        {
            DeclareMember(new(variable.Name, false, variable, IType.BadType, false), variable.Position);
        }

        private static IType RealTypeOr(IType type, IType or) => type is not AutoType ? type : or;

        private void DeclareLocalMember(string name, bool isMutable, IType type, ModulePosition position) =>
            DeclareMember(new(name, isMutable, null, type, false), position);

        private Value CheckCall(CallNode call)
        {
            var variable = SearchForVariable(call.Name);
            if (variable is null)
                return Value.Invalid;

            var type = GetTypeVariable(variable);
            if (type is not FunctionType fnType)
                return ReportNonFunctionType(call);

            if (fnType.ParameterTypes.Length != call.Parameters.Count)
                return ReportWrongNumberOfParameter(call, fnType);

            var canCall = true;
            var isStatic = IsStatic(fnType);

            for (int i = 0; i < call.Parameters.Count; i++)
            {
                var parameter = call.Parameters[i];
                var parameterExpression = CheckExpressionWithContextType(call.Parameters[i], fnType.ParameterTypes[i].Type);

                CheckTypes(fnType.ParameterTypes[i].Type, parameterExpression.Type, parameter.Position);
                PushStaticParameter(ref canCall, fnType.ParameterTypes[i].IsStatic, parameter.Position, parameterExpression);
            }

            if (canCall)
                EvaluateVariable(variable, isStatic);

            return new(fnType.ReturnType, isStatic & fnType.ReturnType is not VoidType ? GetStaticParameter() : null, false);
        }

        private Value ReportWrongNumberOfParameter(CallNode call, FunctionType fnType)
        {
            Tower.Report(call.Parameters.Position, $"Expected '{fnType.ParameterTypes.Length}' parameters, got '{call.Parameters.Count}'");
            return Value.Invalid;
        }

        private Value ReportNonFunctionType(CallNode call)
        {
            Tower.Report(call.Name.Position, $"Expected expression of type 'fn'");
            return Value.Invalid;
        }

        private static bool IsStatic(FunctionType fnType)
        {
            foreach (var parameter in fnType.ParameterTypes)
                if (parameter.IsStatic)
                    return true;

            return false;
        }

        private void PushStaticParameter(ref bool canCall, bool isStatic, ModulePosition position, Value parameterExpression)
        {
            if (parameterExpression.IsConst)
                PushStaticParameter(parameterExpression);
            else if (isStatic)
            {
                Tower.Report(position, "'static' parameter requires constant value");
                canCall = false;
            }
        }

        private void PushStaticParameter(Value staticParameter)
        {
            _staticParameters.Enqueue(staticParameter);
        }

        private Value CheckExpressionWithContextType(INode node, IType type)
        {
            Value result = default;
            DoWithContextType(type, () => { result = CheckExpression(node); });
            return result;
        }

        private void CheckTypes(IType expected, IType got, ModulePosition position)
        {
            if (expected is AutoType or BadType
                || got is AutoType or BadType
                || expected.Equals(got))
                return;

            Tower.Report(position, $"Types mismatch: expected '{expected}', got '{got}'");
        }

        private IType GetTypeVariable(VariableNode variable)
        {
            var t = EvaluateType(variable.Type);
            if (t is AutoType)
                t = CheckTypeExpression(variable.Body);

            return t;
        }

        private IType CheckTypeExpression(INode body)
        {
            var previous = SetSeekType(true);
            var result = CheckExpression(body);
            SetSeekType(previous);
            return result.Type;
        }

        private bool SetSeekType(bool seekType)
        {
            var result = SeekType;
            SeekType = seekType;
            return result;
        }

        private VariableNode SearchForVariable(INode name) => (VariableNode)CheckExpression(name).ConstantValue;

        private static void TypeNode(INode node, IType type)
        {
            node.TypeNode(type);
        }

        private void DeclareParameters(ParameterNode[] parameterList)
        {
            foreach (var parameter in parameterList)
                DeclareParameter(parameter.Name, EvaluateType(parameter.Type), parameter.IsStatic, parameter.Position);
        }

        private Value GetStaticParameter()
        {
            var tmp = _staticParameters.Dequeue();
            return new(tmp.Type, tmp.ConstantValue, true);
        }

        private void DeclareParameter(string name, IType type, bool isStatic, ModulePosition position)
        {
            DeclareMember(new(name, false, isStatic ? GetStaticParameter() : null, type, false), position);
        }

        private bool SearchForInMemory(string name, out MemoryDetail detail)
        {
            for (int i = 0; i < Memory.Count; i++)
            {
                var obj = Memory[i];
                if (obj.Name == name)
                {
                    detail = obj;
                    return obj.IsConst && (CurrentVariable is null || !CurrentVariable.IsLocal || i <= CurrentVariable.DeclarationIndex);
                }
            }

            detail = default;
            return false;
        }

        private void DeclareAllBuiltinMembers()
        {
            DeclareBuiltinMember("void", IType.Void);

            DeclareBuiltinMember("u8", IType.Int(8, false));
            DeclareBuiltinMember("u16", IType.Int(16, false));
            DeclareBuiltinMember("u32", IType.Int(32, false));
            DeclareBuiltinMember("u64", IType.Int(64, false));

            DeclareBuiltinMember("i8", IType.Int(8, true));
            DeclareBuiltinMember("i16", IType.Int(16, true));
            DeclareBuiltinMember("i32", IType.Int(32, true));
            DeclareBuiltinMember("i64", IType.Int(64, true));
        }

        private void DeclareBuiltinMember(string name, IType value) =>
            DeclareMember(new(name, false, value, IType.Type, true), default);

        private void DeclareAllGlobalMembers() => AST.Members.ForEach(member => DeclareGlobalMember(member));

        private void DeclareGlobalMember(VariableNode member)
        {
            if (member is null)
                return;

            ExpectConstantVariable(member);

            var type = EvaluateType(member.Type);
            DeclareMember(new(member.Name, false, member, type is AutoType ? CheckTypeExpression(member.Body) : type, true), member.Position);
        }

        private void ExpectConstantVariable(VariableNode member)
        {
            if (!member.IsConst)
                Tower.Report(member.Position, $"'let' at top level is not allowed");
        }

        private IType EvaluateType(INode type) => type switch
        {
            Token token => EvaluateTypeToken(token),
            FunctionNode function => MakeFunctionType(function, out _),
            BadNode => IType.Auto
        };

        private IType EvaluateTypeToken(Token token) => token.Kind switch
        {
            TokenKind.Identifier => GetTypeFromMemoryObjectOrReport(GetFromMemory(token.Value, token.Position), token.Position)
        };

        private IType GetTypeFromMemoryObjectOrReport(MemoryDetail memoryObject, ModulePosition position)
        {
            if (memoryObject.Type is not TypeType)
            {
                Tower.Report(position, $"Expected expression of type 'type'");
                return memoryObject.Type;
            }
            
            return
                memoryObject.Value is IType t ?
                    t :
                    (IType)GetValueFromLazyEvaluatedVariable((VariableNode)memoryObject.Value).ConstantValue;
        }

        private Value GetValueFromLazyEvaluatedVariable(VariableNode variable)
        {
            return CheckExpression(variable.Body);
        }

        private MemoryDetail GetFromMemory(string name, ModulePosition position)
        {
            if (!SearchForInMemory(name, out var result))
            {
                Tower.Report(position, $"'{name}' not declared");
                return MemoryDetail.Undeclared;
            }

            return result;
        }

        private FunctionType MakeFunctionType(FunctionNode func, out IType returnType) =>
            new(ExtractTypesFromParameters(func.ParameterList), returnType = EvaluateType(func.Type));

        private (IType, bool)[] ExtractTypesFromParameters(ParameterNode[] parameterList)
        {
            var result = new (IType, bool)[parameterList.Length];
            for (int i = 0; i < parameterList.Length; i++)
                result[i] = (EvaluateType(parameterList[i].Type), parameterList[i].IsStatic);

            return result;
        }

        private void DeclareMember(MemoryDetail obj, ModulePosition position)
        {
            Memory.ForEach(o =>
            {
                if (o.Name == obj.Name)
                    Tower.Report(position, $"'{obj.Name}' declared multiple times");
            });

            Memory.Add(obj);
        }

        private void Reset()
        {
            _staticParameters.Clear();
            _contextTypes.Clear();
            _inProgressMembers.Clear();
            SeekType = false;
        }
    }
}
