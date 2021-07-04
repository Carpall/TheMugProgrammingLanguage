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

            IR.EmitGlobalDeclaration(declaration.Name, (LiquorBlock)EvaluateNode(BlockizeNode(declaration.Body)), declaration.Position, declaration.Type);
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

        private void GenerateMember(MemberNode member)
        {
            EmitNode(member.Base);
            EmitInst(new LoadFieldInst(member.Member.Value, member.Member.Position));
        }

        private void GenerateCondition(ConditionalNode cond)
        {
            if (cond.Kind is TokenKind.KeyWhile)
                GenerateWhileNode(cond);
            else
                GenerateIFNode(cond);
        }

        private void GenerateWhileNode(ConditionalNode cond)
        {
            var endLabel = new LabelInst();
            var conditionLabel = new LabelInst();

            LocateLabel(conditionLabel);
            GenerateConditionsExpression(cond.Expression, endLabel);

            EmitBlock(cond.Body);
            EmitJump(conditionLabel);
            
            LocateLabel(endLabel);
        }

        private void GenerateIFNode(ConditionalNode cond)
        {
            /*
             * currentNode: this node is used to walk through else nodes
             * endLabel: this label points to the end of the stastement, used to jump away from condition's blocks
             * nextLabel: the elsenode's label
             */
            var currentNode = cond;
            var endLabel = new LabelInst();
            var nextLabel = GetNextLabel(currentNode, endLabel);

            // while elsenode exists
            for (var count = 0; currentNode is not null; count++)
            {
                // if it's not the first node of the if node
                if (count > 0)
                {
                    LocateLabel(nextLabel);
                    nextLabel = GetNextLabel(currentNode, endLabel);
                }

                // emitting the if expression
                if (!currentNode.IsElse())
                    GenerateConditionsExpression(currentNode.Expression, nextLabel);

                // emitting current node's block
                EmitBlock(currentNode.Body);
                // emitting a jmp to jump away from if block
                EmitJump(endLabel);

                // swapping to next elsenode
                currentNode = currentNode.ElseNode;
            }

            // locating the end label at the end of all the statement
            LocateLabel(endLabel);
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
                EmitInst(new LiquorComptimeVariable(var.Name, (LiquorBlock)EvaluateNode(BlockizeNode(var.Body)), var.Position, var.Type));
                return;
            }

            MaybeReportUninitializedInmutableVariable(var);
            MaybeReportUninitializedUntypedVariable(var);

            EmitInst(new AllocaInst(var.Name, var.IsAssigned(), var.IsMutable, var.Position, var.Type));
            EmitVariableAssign(var);
        }

        private static INode BlockizeNode(INode node)
        {
            var result = new BlockNode { Position = node.Position };
            result.Statements.Add(node);
            return result;
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
                case MemberNode member:
                    GenerateMember(member);
                    break;
                case AssignmentNode assign:
                    GenerateAssign(assign);
                    break;
                default:
                    EmitInst(EvaluateNode(node));
                    break;
            }
        }

        private void GenerateAssign(AssignmentNode assign)
        {
            LowerAssignmentNode(ref assign);

            EmitLeftValue(assign.Name);
            EmitNode(assign.Body);
            EmitInst(new StoreAddressInst(assign.Operator.Position));
        }

        private void LowerAssignmentNode(ref AssignmentNode assign)
        {
            if (assign.Operator.Kind is TokenKind.Equal)
                return;

            assign.Body = new BinaryExpressionNode
            {
                Left = assign.Body,
                Right = assign.Name,
                Operator = GetOperatorFromKindAssignment(assign.Operator),
                Position = assign.Body.Position
            };
        }

        private static Token GetOperatorFromKindAssignment(Token op)
        {
            return Token.NewInfo(op.Kind switch
            {
                TokenKind.AddAssignment => TokenKind.Plus,
                TokenKind.SubAssignment => TokenKind.Minus,
                TokenKind.MulAssignment => TokenKind.Star,
                TokenKind.DivAssignment => TokenKind.Slash,
            }, null, op.Position);
        }

        private void EmitLeftValue(INode name)
        {
            switch (name)
            {
                case MemberNode member:
                    EmitLeftValue(member.Base);
                    EmitInst(new LoadFieldAddressInst(member.Member.Value, member.Position));
                    break;
                case Token token when token.Kind is TokenKind.Identifier:
                    EmitInst(new LoadLocalAddressInst(token.Value, token.Position));
                    break;
                default:
                    Tower.Report(name.Position, $"Expected left value");
                    break;
            }
        }

        private CallInst EvaluateCall(CallNode e)
        {
            EmitNode(e.Name);

            GenerateParameterInCall(e.Parameters);

            return new(e.IsBuiltIn, e.Position);
        }

        private void GenerateParameterInCall(NodeBuilder parameters)
        {
            foreach (var parameter in parameters)
                EmitNode(parameter);
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