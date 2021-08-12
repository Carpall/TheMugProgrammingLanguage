using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mug.Backend.IR;
using Mug.Backend.IR.Values;
using Mug.Compilation;
using Mug.Syntax.AST;
using System.Linq;
using Mug.Grammar;

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
            FunctionNode function => GenerateFunction(function),
            BadNode => new BadIRValue()
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
            GenerateBlock(function.Body),
            GenerateFunctionParameters(function.ParameterList),
            GenerateExpression(function.Type));

        private IRValue[] GenerateFunctionParameters(ParameterNode[] parameterList)
        {
            var result = new IRValue[parameterList.Length];
            for (int i = 0; i < parameterList.Length; i++)
                result[i] = GenerateExpression(parameterList[i].Type);

            return result;
        }

        private IRBlock GenerateBlock(BlockNode body)
        {
            var result = new IRBlock();

            foreach (var statement in body.Statements)
                result.Values.Add(GenerateStatement(statement));

            return result;
        }

        private IRValue GenerateStatement(INode statement) => statement switch
        {
            ReturnNode returnNode => GenerateReturn(returnNode),
            VariableNode variable => GenerateVariable(variable),
        };

        private DeclareVariableInst GenerateVariable(VariableNode variable) => new(
            variable.IsConst ?
                DeclareVariableInst.VariableKind.Const :
                !variable.IsMutable ?
                    DeclareVariableInst.VariableKind.Let :
                    DeclareVariableInst.VariableKind.LetMut,
            variable.Name, GenerateExpression(variable.Type), GenerateExpression(variable.Body));

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
