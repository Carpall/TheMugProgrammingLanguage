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

        private IRBlock CurrentBlock { get; set; }

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

            DeclareFunction(
                variable.Name,
                EvaluateType(variable.Type),
                EvaluateFunctionBlock(GetBlockFromNode(variable.Body)));
        }

        private static BlockNode GetBlockFromNode(INode body) => new(new() { new ReturnNode { Body = body, Position = body.Position } });

        private void DeclareFunction(string name, IRUnsolvedType type, IRBlock block) => IR.Functions.Add((name, type, block));

        private void GenerateExpression(INode node)
        {
            switch (node)
            {
                case MemberNode member:
                    GenerateMember(member);
                    break;
                case Token token:
                    GenerateToken(token);
                    break;
                case BinaryExpressionNode binary:
                    GenerateBinary(binary);
                    break;
                case FunctionNode function:
                    GenerateFunction(function);
                    break;
                case CallNode call:
                    GenerateCall(call);
                    break;
            }
        }

        private void GenerateMember(MemberNode member)
        {
            GenerateExpression(member.Base);
            EmitInstruction(new LoadMemberInst(member.Member.Value, member.Position));
        }

        private void GenerateCall(CallNode call)
        {
            GenerateExpression(call.Name);
            GenerateCallParameters(call.Parameters);
            EmitInstruction(new CallInst((uint)call.Parameters.Count, call.IsBuiltIn, call.Position));
        }

        private void EmitInstruction(IRValue instruction) => CurrentBlock.Values.Add(instruction);

        private void GenerateCallParameters(NodeBuilder parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
                GenerateExpression(parameters[i]);
        }

        private void GenerateBinary(BinaryExpressionNode binary)
        {
            GenerateExpression(binary.Left);
            GenerateExpression(binary.Right);
            EmitInstruction(new BinInst(GetBinInstOpKind(binary), binary.Position));
        }

        private static BinInst.OpKind GetBinInstOpKind(BinaryExpressionNode binary) => binary.Operator.Kind switch
        {
            TokenKind.Plus => BinInst.OpKind.Add,
            TokenKind.Minus => BinInst.OpKind.Sub,
            TokenKind.Star => BinInst.OpKind.Mul,
            TokenKind.Slash => BinInst.OpKind.Div,
        };

        private void GenerateToken(Token token) =>
            EmitInstruction(token.Kind switch
            {
                TokenKind.Identifier => new LoadNameInst(token.Value, token.Position),
                TokenKind.ConstantDigit => new LoadIntegerInst(ulong.Parse(token.Value), token.Position),
                TokenKind.ConstantFloatDigit => new LoadDecimalInst(decimal.Parse(token.Value), token.Position),
                TokenKind.ConstantBoolean => new LoadBooleanInst(bool.Parse(token.Value), token.Position),
                TokenKind.ConstantChar => new LoadCharInst(char.Parse(token.Value), token.Position),
                TokenKind.ConstantString => new LoadStringInst(token.Value, token.Position)
            });

        private void GenerateFunction(FunctionNode function)
        {
            EmitInstruction(new LoadFunctionInst(
                EvaluateFunctionBlock(function.Body, function.ParameterList),
                GetParameterTypes(function.ParameterList),
                EvaluateType(function.Type),
                function.Position));
        }

        private IRUnsolvedType[] GetParameterTypes(ParameterNode[] parameterList)
        {
            var result = new IRUnsolvedType[parameterList.Length];
            for (int i = 0; i < parameterList.Length; i++)
                result[i] = EvaluateType(parameterList[i].Type);

            return result;
        }

        private IRBlock EvaluateFunctionBlock(BlockNode body, ParameterNode[] parameters = null)
        {
            var old = SetupBlock();

            if (parameters is not null) DeclareParameters(parameters);

            foreach (var statement in body.Statements)
                GenerateStatement(statement);

            return RestoreCurrentBlock(old);
        }

        private IRBlock SetupBlock()
        {
            var old = CurrentBlock;
            CurrentBlock = new();
            return old;
        }

        private IRBlock RestoreCurrentBlock(IRBlock old)
        {
            var result = CurrentBlock;
            CurrentBlock = old;
            return result;
        }

        private void DeclareParameters(ParameterNode[] parameters)
        {
            foreach (var parameter in parameters)
            {
                EmitInstruction(new DequeueParameterInst(parameter.Position));
                EmitInstruction(new DeclareVariableInst(
                    DeclareVariableInst.VariableKind.Let,
                    parameter.Name, 
                    EvaluateType(parameter.Type),
                    parameter.Position));
            }
        }

        private IRUnsolvedType EvaluateType(INode type) => 
            type is BadNode or null ?
                IRUnsolvedType.Auto :
                new(EvaluateFunctionBlock(GetBlockFromNode(type)), type.Position);

        private void GenerateStatement(INode statement)
        {
            switch (statement) {
                case ReturnNode returnNode: GenerateReturn(returnNode); break;
                case VariableNode variable: GenerateVariable(variable); break;
            }
        }

        private void GenerateVariable(VariableNode variable)
        {
            GenerateExpression(variable.Body);
            EmitInstruction(new DeclareVariableInst(
                GetVariableKind(variable),
                variable.Name,
                EvaluateType(variable.Type),
                variable.Position));
        }

        private static DeclareVariableInst.VariableKind GetVariableKind(VariableNode variable) =>
            variable.IsConst ?
                DeclareVariableInst.VariableKind.Const :
                !variable.IsMutable ?
                    DeclareVariableInst.VariableKind.Let :
                    DeclareVariableInst.VariableKind.LetMut;

        private void GenerateReturn(ReturnNode returnNode)
        {
            GenerateExpression(returnNode.Body);
            EmitInstruction(new ReturnInst(returnNode.Position));
        }

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
