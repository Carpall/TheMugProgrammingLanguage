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
    // TODO: move in another file with ScopeMemory
    readonly struct MemoryDetail
    {
        public readonly VariableNode Variable;

        public readonly MugValue EvaluatedValue;

        public readonly bool IsGlobal;

        public readonly IType Type;

        public bool IsMutable => Variable.IsMutable;

        public readonly bool IsBuiltin;

        public bool IsAlreadyEvaluated()
        {
            return EvaluatedValue.IsValid;
        }

        public MemoryDetail(VariableNode variable, MugValue evaluatedValue, IType type, bool isGlobal, bool isBuiltin = false)
        {
            Variable = variable;
            EvaluatedValue = evaluatedValue;
            Type = type;
            IsGlobal = isGlobal;
            IsBuiltin = isBuiltin;
        }
    }

    public class SemanticChecker : CompilerComponent
    {
        private const string EntryPointName = "main";

        private NamespaceNode AST { get; set; }

        private Stack<IType> ContextTypes { get; } = new();

        private Stack<Dictionary<string, MemoryDetail>> Scopes { get; } = new();

        private IType ContextType => ContextTypes.Peek();

        private Dictionary<string, MemoryDetail> ScopeMemory => Scopes.Peek();


        // indicates whether the current ast's node is in a function's scope, it's false when the walker is located in a sub scope, ['.' indicates the ast walker's position] example: 'fn { . }' -> true, 'fn { const x = { . } }' -> false
        private bool _isInFirstFunctionBlockScope;

        public SemanticChecker(CompilationInstance tower) : base(tower)
        {
        }

        public void SetAST(NamespaceNode ast)
        {
            AST = ast;
        }

        private void RestoreContextType()
        {
            ContextTypes.Pop();
        }

        private void MakeContextType(IType type)
        {
            ContextTypes.Push(type);
        }

        public NamespaceNode Check()
        {
            Reset();

            // TODO: generate all functions exported
            SetupScope();
            DeclareGlobals();
            SetupBuiltins();
            AnalyzeEntryPoint();
            RestoreScope(true);

            return AST;
        }

        private void SetupBuiltins()
        {
            DeclareBuiltinInt("u8");
            DeclareBuiltinInt("u16");
            DeclareBuiltinInt("u32");
            DeclareBuiltinInt("u64");
            DeclareBuiltinInt("i8");
            DeclareBuiltinInt("i16");
            DeclareBuiltinInt("i32");
            DeclareBuiltinInt("i64");

            DeclareBuiltin("chr", IType.Char);
            DeclareBuiltin("void", IType.Void);

            DeclareBuiltin("type", IType.Type);

            void DeclareBuiltinInt(string name)
            {
                DeclareBuiltin(name, IntType(name));
            }

            static IntType IntType(string name)
            {
                return IType.Int(int.Parse(name[1..]), name[0] == 'i');
            }
        }

        private void DeclareBuiltin(string name, IType type)
        {
            ScopeMemory.Add(name, new(
                variable: null,
                evaluatedValue: new(IType.Type, type, true),
                type: IType.Type,
                isGlobal: true,
                isBuiltin: true));
        }

        private void SetupScope()
        {
            Scopes.Push(GetGlobalMemoryDetailsInScopeMemory());
            _isInFirstFunctionBlockScope = true;
        }

        private Dictionary<string, MemoryDetail> GetGlobalMemoryDetailsInScopeMemory()
        {
            var scopeMemory = Scopes.Count > 0 ? ScopeMemory : new();
            var result = new Dictionary<string, MemoryDetail>();

            foreach (var memoryDetail in scopeMemory)
                if (memoryDetail.Value.IsGlobal)
                    result.Add(memoryDetail.Key, memoryDetail.Value);

            return result;
        }

        private void AnalyzeEntryPoint()
        {
            if (MemoryContainsDefinitionFor(EntryPointName, out var definition))
                AnalyzeVariable(definition.Variable, true);
            else
                CompilationInstance.Throw("Missing entry point");
        }

        private void DeclareGlobals()
        {
            foreach (var global in AST.Members)
            {
                MaybeReportLetAtTopLevel(global);
                DeclareVariable(global, MugValue.Invalid, IType.Void, true);
            }
        }

        private void FixAndCheckTypes(ref IType expectedType, IType gotType, ModulePosition position)
        {
            // infer
            if (expectedType is AutoType)
                expectedType = gotType;

            CheckTypes(expectedType, gotType, position);
        }

        private void DeclareVariable(VariableNode variable, MugValue value, IType type, bool isGlobal)
        {
            if (!ScopeMemory.TryAdd(variable.Name, new(variable, value, type, isGlobal)))
                Tower.Report(variable.Position, $"Variable '{variable.Name}' is already declared");
        }


        private void MaybeReportNonConstantValueForConstantVariable(MugValue value, VariableNode variable)
        {
            if (variable.IsConst && !value.IsConst)
                Tower.Report(variable.Body.Position, $"Expected constant value");
        }

        private void MaybeReportLetAtTopLevel(VariableNode variable)
        {
            if (!variable.IsConst)
                Tower.Report(variable.Position, $"'let' not allowed at top level");
        }

        private IType EvaluateType(INode type)
        {
            if (type is null)
                return IType.BadType;

            var result = type switch
            {
                Token token when token.Kind is TokenKind.Identifier => EvaluateTokenType(token),
                BadNode => IType.Auto,
                _ => ReportUnevaluableType(type.Position)
            };

            TypeNode(type, result);
            return result;
        }

        private IType EvaluateTokenType(Token token)
        {
            var result = EvaluateLocalToken(token);
            if (result.Type is not TypeType)
            {
                Tower.Report(token.Position, $"Expected expression of type 'type'");
                return IType.BadType;
            }

            return ((MugValue)result.ConstantValue).ConstantValue as IType;
        }

        private IType ReportUnevaluableType(ModulePosition position)
        {
            Tower.Report(position, $"Expression is not a type");
            return ContextType;
        }

        private MugValue EvaluateExpression(ref INode node)
        {
            var result = node switch
            {
                null or BadNode => MugValue.Invalid,
                Token expr => EvaluateToken(expr),
                FunctionNode func => EvaluateFunction(func),
                CallNode call => EvaluateCall(call),
                _ => throw new NotImplementedException(),
            };

            ExpectNonVoidType(node.Position, result.Type);
            return result;
        }

        private MugValue EvaluateCall(CallNode call)
        {
            var name = EvaluateExpression(ref call.Name);

            if (!name.IsValid)
                return MugValue.Invalid;

            if (!name.IsConst)
                CompilationInstance.Todo("Function pointer call is not implemented yet");

            if (name.Type is not FunctionType)
            {
                Tower.Report(call.Name.Position, $"Unable to call object of type '{name.Type}'");
                return MugValue.Invalid;
            }

            var function = (FunctionNode)((MugValue)name.ConstantValue).ConstantValue;

            AnalyzeCall(function, call);

            return new(EvaluateType(function.Type), null, false);
        }

        private void AnalyzeCall(FunctionNode function, CallNode call)
        {
            if (function.ParameterList.Length != call.Parameters.Count)
            {
                Tower.Report(call.Parameters.Position, $"Expected '{function.ParameterList.Length}' parameters, got '{call.Parameters.Count}'");
                return;
            }

            for (int i = 0; i < function.ParameterList.Length; i++)
            {
                var functionParameter = function.ParameterList[i];
                var inputParameter = call.Parameters[i];
                var evalautedInputParameter = EvaluateExpression(ref inputParameter);
                call.Parameters[i] = inputParameter;

                if (functionParameter.IsStatic && !evalautedInputParameter.IsConst)
                    Tower.Report(inputParameter.Position, $"Static parameters require constant values");

                CheckTypes(EvaluateType(functionParameter.Type), evalautedInputParameter.Type, inputParameter.Position);
            }
        }

        private MugValue EvaluateFunction(FunctionNode func)
        {
            SetupScope();
            var fnType = IType.Fn(EvaluateParameterTypes(func.ParameterList), EvaluateType(func.Type));

            MakeContextType(fnType.ReturnType);
            AnalyzeFunctionBody(func.Body);
            RestoreContextType();
            RestoreScope(true);

            TypeNode(func, fnType);
            return new(fnType, func, true);
        }

        private static void TypeNode(INode node, IType type)
        {
            node.TypeNode(type);
        }

        private void AnalyzeFunctionBody(BlockNode body)
        {
            for (int i = 0; i < body.Statements.Count; i++)
            {
                var statement = body.Statements[i];
                AnalyzeStatement(ref statement, i == body.Statements.Count - 1);
                body.Statements[i] = statement;
            }
        }

        private void AnalyzeStatement(ref INode statement, bool isLast)
        {
            switch (statement)
            {
                case VariableNode var:
                    AnalyzeVariable(var);
                    break;
                case CallNode call when !isLast || ContextType is VoidType:
                    EvaluateCall(call);
                    break;
                default:
                    AnalyzeExpression(ref statement, isLast);
                    break;
            }
        }

        private void AnalyzeExpression(ref INode statement, bool isLast)
        {
            MaybeReportExpressionNotLastInBlock(isLast, statement.Position);
            var value = EvaluateExpression(ref statement);
            LowerImplicitReturn(ref statement);
            CheckTypes(ContextType, value.Type, statement.Position);
        }

        private void MaybeReportExpressionNotLastInBlock(bool isLast, ModulePosition position)
        {
            if (!isLast)
                Tower.Report(position, $"Expressions must be located at the end of the block");
        }

        private void CheckTypes(IType expectedType, IType gotType, ModulePosition position)
        {
            // bad type is automatically skipped because it doesn't need to be checked: badtypes are already reported when occurred and it's ugly to see 'types mismatch: type badtype'
            if (expectedType is BadType || gotType is BadType)
                return;

            // checking the type's compatibility
            if (!gotType.Equals(expectedType))
                Tower.Report(position, $"Types mismatch: expected '{expectedType}', got '{gotType}'");
        }

        private void LowerImplicitReturn(ref INode statement)
        {
            if (!_isInFirstFunctionBlockScope)
                return;

            statement = new ReturnNode
            {
                Body = statement,
                Position = statement.Position
            };
        }

        private void AnalyzeVariable(VariableNode variable, bool redeclare = false)
        {
            if (!redeclare & variable.IsConst)
                CompilationInstance.Todo("const is not allowed in local yet");

            var type = EvaluateType(variable.Type);
            ExpectNonVoidType(variable.Type.Position, type);

            MakeContextType(type);
            var value = EvaluateExpression(ref variable.Body);
            RestoreContextType();

            FixAndCheckTypes(ref type, value.Type, variable.Body.Position);

            MaybeReportNonConstantValueForConstantVariable(value, variable);
            MaybeReportSpecialValueForNonConstantVariable(value, variable);

            if (redeclare)
                RedeclareVariable(variable, value, type , redeclare);
            else
                DeclareVariable(variable, value, type, redeclare);

            TypeNode(variable, type);
        }

        private void ExpectNonVoidType(ModulePosition position, IType type)
        {
            if (type is VoidType)
                Tower.Report(position, $"Expected non void type");
        }

        private void RedeclareVariable(VariableNode variable, MugValue value, IType type, bool isGlobal)
        {
            ScopeMemory[variable.Name] = new(variable, value, type, isGlobal);
        }

        private void MaybeReportSpecialValueForNonConstantVariable(MugValue value, VariableNode variable)
        {
            if (!variable.IsConst & value.IsSpecial())
                Tower.Report(variable.Body.Position, $"Type '{value.Type}' requires a 'const' context");
        }

        private void MakeScope(out bool wasInFirstFunctionBlockScope)
        {
            Scopes.Push(new(ScopeMemory));
            wasInFirstFunctionBlockScope = _isInFirstFunctionBlockScope;
            _isInFirstFunctionBlockScope = false;
        }

        private void RestoreScope(bool wasInFirstFunctionBlockScope)
        {
            Scopes.Pop();
            _isInFirstFunctionBlockScope = wasInFirstFunctionBlockScope;
        }

        private IType[] EvaluateParameterTypes(ParameterNode[] parameters)
        {
            var result = new IType[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var variableFromParameter = GetVariableFromParameter(parameter);
                var parameterValue = new MugValue(EvaluateType(variableFromParameter.Type), null, false);

                DeclareVariable(variableFromParameter, parameterValue, parameterValue.Type, false);
                result[i] = EvaluateType(parameter.Type);
            }

            return result;
        }

        private static VariableNode GetVariableFromParameter(ParameterNode parameter)
        {
            return new VariableNode
            {
                IsMutable = false,
                IsConst = false,
                Name = parameter.Name,
                Type = parameter.Type,
                Position = parameter.Position
            };
        }

        private MugValue EvaluateToken(Token token)
        {
            if (token.Kind is TokenKind.Identifier)
                return EvaluateLocalToken(token);

            var type = GetTypeFromConstantTokenKind(token.Kind);
            TypeNode(token, type);
            return new(type, token.Value, true);
        }

        private MugValue EvaluateLocalToken(Token token)
        {
            var definitionNullable = GetDefinition(token.Value, token.Position);
            if (!definitionNullable.HasValue)
                return MugValue.Invalid;

            var definition = definitionNullable.Value;
            if (!definition.IsAlreadyEvaluated() & !definition.IsBuiltin)
                EvaluateDefinition(ref definition);

            return new(definition.Type, definition.EvaluatedValue, definition.Variable?.IsConst ?? true);
        }

        private void EvaluateDefinition(ref MemoryDetail definition)
        {
            AnalyzeVariable(definition.Variable, true);

            var newDefinition = GetDefinition(definition.Variable.Name, definition.Variable.Position).Value;

            definition = new(null, newDefinition.EvaluatedValue, newDefinition.Type, true);
        }

        private MemoryDetail? GetDefinition(string value, ModulePosition position)
        {
            if (MemoryContainsDefinitionFor(value, out var variable))
                return variable;

            Tower.Report(position, $"Variable '{value}' is not declared");
            return null;
        }

        private bool MemoryContainsDefinitionFor(string value, out MemoryDetail result)
        {
            return ScopeMemory.TryGetValue(value, out result);
        }

        private IType GetTypeFromConstantTokenKind(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.ConstantDigit => IntContextTypeOr(IType.Int(32, true))
            };
        }

        private IntType IntContextTypeOr(IntType ortype)
        {
            return ContextType is IntType type ? type : ortype;
        }

        private void Reset()
        {
        }
    }
}
