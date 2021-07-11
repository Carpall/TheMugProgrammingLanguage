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

        public bool IsMutable => Variable.IsMutable;

        public IType Type => EvaluatedValue?.Type ?? IType.BadType;

        public bool IsBuiltin { get; }

        public bool IsAlreadyEvaluated()
        {
            return EvaluatedValue is not null;
        }

        public MemoryDetail(VariableNode variable, MugValue evaluatedValue, bool isGlobal, bool isBuiltin = false)
        {
            Variable = variable;
            EvaluatedValue = evaluatedValue;
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
            RestoreScope();

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
            ScopeMemory.Add(name, new(null, new(IType.Type, type, true), true, true));
        }

        private void SetupScope()
        {
            Scopes.Push(GetGlobalMemoryDetailsInScopeMemory());
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
                DeclareVariable(global, null, true);
            }
        }

        private void FixAndCheckTypes(ref IType type, MugValue value, ModulePosition position)
        {
            if (type is BadType)
                return;

            if (type is AutoType)
                type = value.Type;

            if (value.Type.GetType() != type.GetType()
                && value.Type != type)
                Tower.Report(position, $"Types mismatch: expected '{type}', got '{value.Type}'");
        }

        private void DeclareVariable(VariableNode variable, MugValue value, bool isGlobal)
        {
            if (!ScopeMemory.TryAdd(variable.Name, new(variable, value, isGlobal)))
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

            return (result.ConstantValue as MugValue).ConstantValue as IType;
        }

        private IType ReportUnevaluableType(ModulePosition position)
        {
            Tower.Report(position, $"Expression is not a type");
            return ContextType;
        }

        private MugValue EvaluateExpression(INode node) => node switch
        {
            Token expr => EvaluateToken(expr),
            FunctionNode func => EvaluateFunction(func),
            _ => throw new NotImplementedException(),
        };

        private MugValue EvaluateFunction(FunctionNode func)
        {
            SetupScope();
            var fnType = IType.Fn(EvaluateParameterTypes(func.ParameterList), EvaluateType(func.Type));

            MakeContextType(fnType.ReturnType);
            AnalyzeFunctionBody(func.Body);
            RestoreContextType();
            RestoreScope();

            TypeNode(func, fnType);
            return new(fnType, func, true);
        }

        private static void TypeNode(INode node, IType type)
        {
            node.TypeNode(type);
        }

        private void AnalyzeFunctionBody(BlockNode body)
        {
            foreach (var statement in body.Statements)
                AnalyzeStatement(statement);
        }

        private void AnalyzeStatement(INode statement)
        {
            switch (statement)
            {
                case VariableNode var:
                    AnalyzeVariable(var);
                    break;
                default:
                    // TODO: add hidden control flow to return or load onto the stack the value
                    EvaluateExpression(statement);
                    break;
            }
        }

        private void AnalyzeVariable(VariableNode variable, bool redeclare = false)
        {
            if (!redeclare & variable.IsConst)
                CompilationInstance.Todo("const is not allowed in local yet");

            var type = EvaluateType(variable.Type);

            MakeContextType(type);
            var value = EvaluateExpression(variable.Body);
            RestoreContextType();

            FixAndCheckTypes(ref type, value, variable.Body.Position);

            MaybeReportNonConstantValueForConstantVariable(value, variable);
            MaybeReportSpecialValueForNonConstantVariable(value, variable);

            if (redeclare)
                RedeclareVariable(variable, value, redeclare);
            else
                DeclareVariable(variable, value, redeclare);

            TypeNode(variable, type);
        }

        private void RedeclareVariable(VariableNode variable, MugValue value, bool isGlobal)
        {
            ScopeMemory[variable.Name] = new(variable, value, isGlobal);
        }

        private void MaybeReportSpecialValueForNonConstantVariable(MugValue value, VariableNode variable)
        {
            if (!variable.IsConst & value.IsSpecial)
                Tower.Report(variable.Body.Position, $"Type '{value.Type}' requires a 'const' context");
        }

        private void MakeScope()
        {
            Scopes.Push(new(ScopeMemory));
        }

        private void RestoreScope()
        {
            Scopes.Pop();
        }

        private IType[] EvaluateParameterTypes(ParameterNode[] parameters)
        {
            var result = new IType[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var variableFromParameter = GetVariableFromParameter(parameter);

                DeclareVariable(variableFromParameter, null, false);
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
            var definition = GetDefinition(token.Value, token.Position);
            if (!definition.IsAlreadyEvaluated() & !definition.IsBuiltin)
                EvaluateDefinition(ref definition);

            return new(definition.Type, definition.EvaluatedValue, definition.Variable?.IsConst ?? true);
        }

        private void EvaluateDefinition(ref MemoryDetail definition)
        {
            AnalyzeVariable(definition.Variable, true);
            var definitionValue = GetDefinition(definition.Variable.Name, definition.Variable.Position).EvaluatedValue.ConstantValue as MugValue;
            definition = new(null, definitionValue, true);
        }

        private MemoryDetail GetDefinition(string value, ModulePosition position)
        {
            if (MemoryContainsDefinitionFor(value, out var variable))
                return variable;

            Tower.Report(position, $"Variable '{value}' is not declared");
            return new(new(), MugValue.BadValue, false);
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
