using Mug.Compilation;
using Mug.Lexer;
using Mug.Parser.AST;
using Mug.Parser.AST.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Parser.ASTLowerer
{
    class Lowerer : CompilerComponent
    {
        public Lowerer(CompilationTower tower) : base(tower)
        {
        }

        public AssignmentStatement LowerPostfixStatementToAssignment(PostfixOperator postfixoperator)
        {
            return new AssignmentStatement
            {
                Name = postfixoperator.Expression,
                Operator = new Token(TokenKind.Equal, null, postfixoperator.Position, false),
                Position = postfixoperator.Position,
                Body = new BinaryExpressionNode()
                {
                    Left = postfixoperator.Expression,
                    Right = new Token(TokenKind.ConstantDigit, "1", postfixoperator.Expression.Position, false),
                    Operator = new Token(PostfixOperatorKindToOperator(postfixoperator.Postfix.Kind), null, postfixoperator.Postfix.Position, false),
                    Position = postfixoperator.Postfix.Position
                }
            };
        }

        private static TokenKind PostfixOperatorKindToOperator(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.OperatorIncrement => TokenKind.Plus,
                TokenKind.OperatorDecrement => TokenKind.Minus,
            };
        }

        public void LowerAssignmentStatementOperator(ref AssignmentStatement statement)
        {
            if (statement.Operator.Kind is TokenKind.Equal)
                return;

            var toLower = statement.Operator.Kind;

            statement.Operator = new Token(TokenKind.Equal, null, statement.Operator.Position, false);
            statement.Body = new BinaryExpressionNode()
            {
                Left = statement.Name,
                Right = statement.Body,
                Position = statement.Position,
                Operator = new Token(AssignmentOperatorKindToOperator(toLower), null, statement.Operator.Position, false)
            };
        }

        private static TokenKind AssignmentOperatorKindToOperator(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.AddAssignment => TokenKind.Plus,
                TokenKind.SubAssignment => TokenKind.Minus,
                TokenKind.MulAssignment => TokenKind.Star,
                TokenKind.DivAssignment => TokenKind.Slash
            };
        }
    }
}
