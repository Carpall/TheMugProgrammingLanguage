using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mug.Backend.IR;
using Mug.Backend.IR.Values;
using Mug.Compilation;
using Mug.Syntax.AST;
using Mug.Grammar;
using Mug.Syntax;

namespace Mug.Backend
{
    public class Generator : CompilerComponent
    {
        private NamespaceNode AST { get; set; }

        public MugIR IR { get; private set; }

        public Generator(CompilationInstance tower) : base(tower)
        {
        }

        public void SetAST(NamespaceNode ast)
        {
            AST = ast;
        }

        public MugIR Generate()
        {
            Reset();

            GenerateGlobals();

            return IR;
        }

        private void GenerateGlobals()
        {
            foreach (var global in AST.Members)
                GenerateGloblaVariable(global);
        }

        private void GenerateGloblaVariable(VariableNode variable)
        {
            ExpectConstAtTopLevel(variable);

            DeclareFunction(variable.Name, GenerateExpression(variable.Type), GenerateExpression(variable.Body));
        }

        private void DeclareFunction(string name, IRValue type, IRValue value) => IR.Functions.Add((name, type, value));

        private IRValue GenerateExpression(INode node) => node switch
        {
            Token token => GenerateToken(token),
            BinaryExpressionNode binary => GenerateBinary(binary),
            FunctionNode function => GenerateFunction(function),
            CallNode call => GenerateCall(call),
            BadNode => new BadIRValue()
        };
        
        private CallInst GenerateCall(CallNode call) => new(
            GenerateExpression(call.Name),
            GenerateCallParameters(call.Parameters));

        private IRValue[] GenerateCallParameters(NodeBuilder parameters)
        {
            var result = new IRValue[parameters.Count];

            for (int i = 0; i < parameters.Count; i++)
                result[i] = GenerateExpression(parameters[i]);

            return result;
        }

        private BinInst GenerateBinary(BinaryExpressionNode binary) => new(
            GetBinInstOpKind(binary), GenerateExpression(binary.Left), GenerateExpression(binary.Right));

        private BinInst.OpKind GetBinInstOpKind(BinaryExpressionNode binary) => binary.Operator.Kind switch
        {
            TokenKind.Plus => BinInst.OpKind.Add,
            TokenKind.Minus => BinInst.OpKind.Sub,
            TokenKind.Star => BinInst.OpKind.Mul,
            TokenKind.Slash => BinInst.OpKind.Div,
        };

        private static IRValue GenerateToken(Token token) => token.Kind switch
        {
            TokenKind.Identifier => new NameValue(token.Value),
            TokenKind.ConstantDigit => new IntegerValue(ulong.Parse(token.Value)),
            TokenKind.ConstantFloatDigit => new DecimalValue(decimal.Parse(token.Value)),
            TokenKind.ConstantBoolean => new BooleanValue(bool.Parse(token.Value)),
            TokenKind.ConstantChar => new CharValue(char.Parse(token.Value)),
            TokenKind.ConstantString => new StringValue(token.Value)
        };

        private FunctionValue GenerateFunction(FunctionNode function) => new(
            GenerateBlock(function.Body, function.ParameterList),
            GenerateFunctionParameters(function.ParameterList),
            GenerateExpression(function.Type));

        private IRValue[] GenerateFunctionParameters(ParameterNode[] parameterList)
        {
            var result = new IRValue[parameterList.Length];
            for (int i = 0; i < parameterList.Length; i++)
                result[i] = GenerateExpression(parameterList[i].Type);

            return result;
        }

        private IRBlock GenerateBlock(BlockNode body, ParameterNode[] parameters)
        {
            var result = new IRBlock();

            DeclareParameters(result, parameters);

            foreach (var statement in body.Statements)
                result.Values.Add(GenerateStatement(statement));

            return result;
        }

        private void DeclareParameters(IRBlock result, ParameterNode[] parameters)
        {
            foreach (var parameter in parameters)
                result.Values.Add(new DeclareVariableInst(
                    DeclareVariableInst.VariableKind.Let,
                    parameter.Name,
                    GenerateExpression(parameter.Type),
                    new DequeueParameterInst()));
        }

        private IRValue GenerateStatement(INode statement) => statement switch
        {
            ReturnNode returnNode => GenerateReturn(returnNode),
            VariableNode variable => GenerateVariable(variable),
        };

        private DeclareVariableInst GenerateVariable(VariableNode variable) => new(
            GetVariableKind(variable),
            variable.Name,
            GenerateExpression(variable.Type),
            GenerateExpression(variable.Body));

        private DeclareVariableInst.VariableKind GetVariableKind(VariableNode variable) =>
            variable.IsConst ?
                DeclareVariableInst.VariableKind.Const :
                !variable.IsMutable ?
                    DeclareVariableInst.VariableKind.Let :
                    DeclareVariableInst.VariableKind.LetMut;

        private ReturnInst GenerateReturn(ReturnNode returnNode) => new(GenerateExpression(returnNode.Body));

        private void ExpectConstAtTopLevel(VariableNode variable)
        {
            if (!variable.IsConst)
                Tower.Report(variable.Position, $"'let' not allowed at top level");
        }

        private void Reset()
        {
            IR = new(Tower.OutputFilename);
        }
    }
}
