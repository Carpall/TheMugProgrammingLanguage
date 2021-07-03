using Mug.AstGeneration.IR;
using Mug.AstGeneration.IR.Values;
using Mug.AstGeneration.IR.Values.Instructions;
using Mug.Compilation;
using Mug.Grammar;
using Mug.Syntax;
using Mug.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mug.AstGeneration
{
    public class AstGenerator : CompilerComponent
    {
        public NamespaceNode AST { get; set; }

        private LiquorIR IR { get; set; }

        private LiquorBlock CurrentBlock;

        public AstGenerator(CompilationInstance tower) : base(tower)
        {
        }

        public void SetAST(NamespaceNode ast)
        {
            AST = ast;
        }

        public LiquorIR Generate()
        {
            Reset();

            WalkGlobalScope();

            return IR;
        }

        private void WalkGlobalScope()
        {
            foreach (var declaration in AST.Members)
                EmitGlobalScopeMember(declaration);
        }

        private void EmitGlobalScopeMember(VariableNode declaration)
        {
            MaybeReportNonConstDeclaration(declaration);

            IR.EmitGlobalDeclaration(declaration.Name, EvaluateNode(declaration.Body), declaration.Position, declaration.Type);
        }

        private ILiquorValue EvaluateNode(INode node) => node switch
        {
            Token token => EvaluateToken(token),
            FunctionNode func => EvaluateFunction(func),
            CallNode call => EvaluateCall(call),
            BinaryExpressionNode bin => EvaluateBinary(bin),
            BlockNode block => EvaluateBlock(block),
            BadNode => null
        };

        private void GenerateCondition(ConditionalNode cond)
        {
            var currentNode = cond;
            var end = new LabelInst();
            var nextLabel = GetNextLabel(currentNode, end);

            for (var count = 0; currentNode is not null; count++)
            {
                if (count > 0)
                {
                    LocateLabel(nextLabel);
                    nextLabel = GetNextLabel(currentNode, end);
                }

                if (!currentNode.IsElse())
                    GenerateConditionsExpression(currentNode.Expression, nextLabel);

                EmitBlock(currentNode.Body);
                EmitJump(end);

                currentNode = currentNode.ElseNode;
            }

            LocateLabel(end);
        }

        private static LabelInst GetNextLabel(ConditionalNode currentNode, LabelInst end)
        {
            return currentNode.ElseNode is not null ? new LabelInst() : end;
        }

        private void LocateLabel(LabelInst label)
        {
            label.Index = CurrentBlock.Instructions.Count;
            EmitInst(label);
        }

        private void EmitJump(LabelInst label)
        {
            EmitInst(new JumpInst(label));
        }

        private void GenerateConditionsExpression(INode expression, LabelInst nextLabel)
        {
            EmitNode(expression);

            EmitJumpCondition(false, nextLabel);
        }

        private void EmitJumpCondition(bool condition, LabelInst nextLabel)
        {
            EmitInst(new JumpConditionInst(nextLabel, condition));
        }

        private void EmitBlock(BlockNode block)
        {
            foreach (var node in block.Statements)
                EmitNode(node);
        }

        private void EmitVariable(VariableNode var)
        {
            if (var.IsConst)
            {
                EmitInst(new LiquorComptimeVariable(var.Name, EvaluateNode(var.Body), var.Position, var.Type));
                return;
            }

            MaybeReportUninitializedInmutableVariable(var);
            MaybeReportUninitializedUntypedVariable(var);

            EmitInst(new AllocaInst(var.Name, var.IsAssigned(), var.IsMutable, var.Position, var.Type));
            EmitVariableAssign(var);
        }

        private void MaybeReportUninitializedUntypedVariable(VariableNode var)
        {
            if (TypeIsAuto(var.Type) && !var.IsAssigned())
                Tower.Report(var.Position, "Type notation needed");
        }

        private static bool TypeIsAuto(INode type)
        {
            return type is BadNode;
        }

        private void EmitVariableAssign(VariableNode var)
        {
            if (var.IsAssigned())
            {
                EmitNode(var.Body);
                EmitInst(new StoreLocalInst(var.Name, var.Body.Position));
            }
        }

        private void MaybeReportUninitializedInmutableVariable(VariableNode var)
        {
            if (!var.IsMutable && !var.IsAssigned())
                Tower.Report(var.Position, $"Inmutable variable needs to be initialized");
        }

        private void EmitInst(ILiquorValue inst)
        {
            Debug.Assert(inst is not null);
            CurrentBlock.Instructions.Add(inst);
        }

        private BinaryInst EvaluateBinary(BinaryExpressionNode bin)
        {
            EmitNode(bin.Left);
            EmitNode(bin.Right);
            return new(TokenKindToBinaryKind(bin.Operator.Kind), bin.Position);
        }

        private static LiquorBinaryInstKind TokenKindToBinaryKind(TokenKind kind) => kind switch
        {
            TokenKind.Plus => LiquorBinaryInstKind.Add,
            TokenKind.Minus => LiquorBinaryInstKind.Sub,
            TokenKind.Star => LiquorBinaryInstKind.Mul,
            TokenKind.Slash => LiquorBinaryInstKind.Div,
            TokenKind.BooleanEQ => LiquorBinaryInstKind.Eq,
            TokenKind.BooleanNEQ => LiquorBinaryInstKind.Ne,
            TokenKind.BooleanGreater => LiquorBinaryInstKind.Gt,
            TokenKind.BooleanLess => LiquorBinaryInstKind.Lt,
            TokenKind.BooleanGEQ => LiquorBinaryInstKind.Ge,
            TokenKind.BooleanLEQ => LiquorBinaryInstKind.Le,
        };


        private void EmitNode(INode node)
        {
            switch (node)
            {
                case VariableNode var:
                    EmitVariable(var);
                    break;
                case ConditionalNode cond:
                    GenerateCondition(cond);
                    break;
                default:
                    EmitInst(EvaluateNode(node));
                    break;
            }
        }

        private CallInst EvaluateCall(CallNode e)
        {
            var name = GetCallNameHead(e.Name, out var toevaluate);
            if (toevaluate is not null)
                EmitNode(toevaluate);

            GenerateParameterInCall(e.Parameters);

            return new(name, toevaluate is not null & name is not null, e.IsBuiltIn, e.Position);
        }

        private void GenerateParameterInCall(NodeBuilder parameters)
        {
            foreach (var parameter in parameters)
                EmitNode(parameter);
        }

        private static string GetCallNameHead(INode name, out INode toevaluate)
        {
            switch (name)
            {
                case Token token:
                    toevaluate = null;
                    return token.Value;
                case MemberNode member:
                    toevaluate = member.Base;
                    return member.Member.Value;
                default: toevaluate = name; return null;
            };
        }

        private LiquorFunction EvaluateFunction(FunctionNode e)
        {
            var body = EvaluateBlock(e.Body);

            return new(null, body, GetParameterListTypes(e.ParameterList), e.Type, e.Position);
        }

        private static INode[] GetParameterListTypes(ParameterNode[] parameterList)
        {
            var result = new INode[parameterList.Length];

            for (int i = 0; i < parameterList.Length; i++)
                result[i] = parameterList[i].Type;

            return result;
        }

        private LiquorBlock EvaluateBlock(BlockNode body)
        {
            var oldBlock = SetupBlock(new());

            EmitBlock(body);

            return RestoreBlock(oldBlock);
        }

        private LiquorBlock RestoreBlock(LiquorBlock oldBlock)
        {
            var result = CurrentBlock;
            CurrentBlock = oldBlock;
            return result;
        }

        private LiquorBlock SetupBlock(LiquorBlock block)
        {
            var oldBlock = CurrentBlock;
            CurrentBlock = block;
            return oldBlock;
        }

        private static ILiquorValue EvaluateToken(Token e)
        {
            return
                e.Kind is TokenKind.Identifier ?
                    EvaluateIdentifier(e) :
                    EvaluateConstantToken(e);
        }

        private static LoadLocalInst EvaluateIdentifier(Token e)
        {
            return new(e.Value, e.Position);
        }

        private static LoadConstantInst EvaluateConstantToken(Token e)
        {
            ConstantTokenToLiquorConstant(e.Kind, e.Value, out var kind, out var value);

            return new(kind, value, e.Position);
        }

        private static void ConstantTokenToLiquorConstant(TokenKind kind, string value, out LiquorConstantKind lkind, out object lvalue)
        {
            switch (kind)
            {
                case TokenKind.ConstantDigit:
                    lkind = LiquorConstantKind.Integer;
                    lvalue = ulong.Parse(value);
                    break;
                case TokenKind.ConstantChar:
                    lkind = LiquorConstantKind.Character;
                    lvalue = value.First();
                    break;
                case TokenKind.ConstantBoolean:
                    lkind = LiquorConstantKind.Boolean;
                    lvalue = bool.Parse(value);
                    break;
                case TokenKind.ConstantFloatDigit:
                    lkind = LiquorConstantKind.FloatingPoint;
                    lvalue = decimal.Parse(value.Replace('.', ','));
                    break;
                case TokenKind.ConstantString:
                    lkind = LiquorConstantKind.String;
                    lvalue = value;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void MaybeReportNonConstDeclaration(VariableNode declaration)
        {
            if (!declaration.IsConst)
                Tower.Report(declaration.Position, "'let' not allowed at top level");
        }

        private void Reset()
        {
            IR = new();
        }
    }
}