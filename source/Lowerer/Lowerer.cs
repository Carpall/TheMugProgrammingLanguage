using Mug.Models.Generator;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using Mug.Models.Parser.NodeKinds.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mug.Models.Lowerering
{
    public static class Lowerer
    {
        private static INode AddTerm(INode expression, MatchNode matchnode)
        {
            // fn: addterm(expr, match)
            // if match.kind = | or &
            //  expr <- node_call(match.expr.kind = | then or! else and!, or, (addterm(match.expr.left, match), addterm(match.expr.right, match)))
            // else
            //  expr <- node_bin(match.expr.left, ==, match.expr.right)
            // ret <- expr

            if (matchnode.Expression is BooleanExpressionNode boolexpr &&
                        boolexpr.Operator == TokenKind.BooleanOR | boolexpr.Operator == TokenKind.BooleanAND)
            {
                expression = new CallStatement()
                {
                    Position = matchnode.Position,
                    IsBuiltIn = true,
                    Name = new Token(
                        TokenKind.Identifier,
                        new string(boolexpr.Operator.ToString().Skip(7).ToArray()).ToLower(),
                        matchnode.Position,
                        false),
                    Parameters = new NodeBuilder()
                    {
                        Nodes = new()
                        {
                            AddTerm(expression, new MatchNode()
                            {
                                Position = matchnode.Position,
                                Expression = boolexpr.Left,
                                Body = matchnode.Body
                            }),

                            AddTerm(expression, new MatchNode()
                            {
                                Position = matchnode.Position,
                                Expression = boolexpr.Right,
                                Body = matchnode.Body
                            }),
                        }
                    }
                };
            }
            else
                expression = new BooleanExpressionNode()
                {
                    Position = matchnode.Expression.Position,
                    Left = expression,
                    Right = matchnode.Expression,
                    Operator = TokenKind.BooleanEQ
                };

            return expression;
        }

        public static bool LowerMatchExpression(
            MatchExpression matchexpr,
            out ConditionalStatement conditional,
            LocalGenerator localgenerator)
        {
            conditional = new();
            var temp = conditional;
            var foundElseNode = false;
            var i = 0;
            var result = true;

            for (; i < matchexpr.Body.Count; i++)
            {
                var matchnode = matchexpr.Body[i];
                if (matchnode.IsElseNode && foundElseNode)
                    result = localgenerator.Report(matchexpr.Position, "'else' node already declared in 'match' expression");

                foundElseNode = matchnode.IsElseNode;
                temp.ElseNode = new ConditionalStatement()
                {
                    Position = matchexpr.Position,
                    Kind = i == 0 ? TokenKind.KeyIf : matchnode.IsElseNode ? TokenKind.KeyElse : TokenKind.KeyElif,
                    Expression = addCase(ref matchnode),
                    Body = matchnode.Body,
                };

                temp = temp.ElseNode;
            }

            if (i == 1 && foundElseNode)
                result = localgenerator.Report(matchexpr.Body.First().Position, "'else' node must have predecessor cases");
            else if (i == 0)
                result = localgenerator.Report(matchexpr.Position, "Expected at least one match node");

            conditional = conditional.ElseNode;
            return result;

            // to inline
            INode addCase(ref MatchNode matchnode)
            {
                if (!matchnode.IsElseNode)
                    return AddTerm(matchexpr.Expression, matchnode);

                return null;
            }
        }
    }
}
