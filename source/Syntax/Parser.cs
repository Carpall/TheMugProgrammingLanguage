using Mug.Compilation;
using Mug.Grammar;
using Mug.Syntax.AST;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Data;
using System.Collections.Immutable;

namespace Mug.Syntax
{
    public class Parser : CompilerComponent
    {
        public NamespaceNode Head { get; set; }

        private int CurrentIndex { get; set; }

        private ImmutableArray<Token> Tokens { get; set; }

        private Pragmas _pragmas = null;

        private ImmutableArray<TokenKind>.Builder _modifiers = ImmutableArray.CreateBuilder<TokenKind>(2);

        public void SetTokens(ImmutableArray<Token> tokens)
        {
            Tokens = tokens;
        }

        private void ParseError(string error)
        {
            if (Match(TokenKind.EOF))
                ParseErrorEOF(error);

            Tower.Throw(Current, error);
        }

        private void ParseError(ModulePosition position, string error)
        {
            if (Match(TokenKind.EOF))
                ParseErrorEOF(error);

            Tower.Throw(position, error);
        }

        private void ParseErrorEOF(string error)
        {
            Tower.Throw(Back, $"Unexpected <EOF>{(error != "" ? $": {error}" : "")}");
        }

        public Parser(CompilationInstance tower) : base(tower)
        {
        }

        private Token Current
        {
            get
            {
                if (CurrentIndex >= Tokens.Length)
                    ParseErrorEOF("");

                return Tokens[CurrentIndex];
            }
        }

        private Token Back
        {
            get
            {
                if (CurrentIndex - 1 >= 0)
                    return Tokens[CurrentIndex - 1];

                return new Token();
            }
        }

        private string UnexpectedToken => $"Unexpected token '{Current.Value}'";

        private static string TokenKindsToString(TokenKind[] kinds)
        {
            StringBuilder result = new();

            for (var i = 0; i < kinds.Length; i++)
            {
                result.Append(kinds[i].GetDescription());
                if (i < kinds.Length - 1)
                    result.Append("', '");
            }

            return result.ToString();
        }

        private Token ExpectMultiple(string error, params TokenKind[] kinds)
        {
            if (kinds.Length > 0)
            {
                for (var i = 0; i < kinds.Length; i++)
                    if (Match(kinds[i]))
                    {
                        Advance();
                        return Back;
                    }

                if (error == "")
                    Report($"Expected '{TokenKindsToString(kinds)}', found '{Current.Value}'");
                else
                    Report(error);
            } 

            return Token.NewInfo(kinds.FirstOrDefault(), "");
        }

        private Token Expect(TokenKind kind, string error = null)
        {
            if (!Match(kind))
            {
                if (error is null)
                    Report($"Expected '{kind.GetDescription()}', found '{Current.Value}'");
                else
                    Report(error);
            }

            Advance();
            return Back;
        }

        private bool Match(TokenKind kind, bool linesensitive = false)
        {
            if (linesensitive && Current.IsOnNewLine)
                return false;

            return Current.Kind == kind;
        }

        private bool MatchAdvance(TokenKind kind, bool linesensitive = false)
        {
            var match = Match(kind, linesensitive);

            if (match)
                Advance();

            return match;
        }

        private bool MatchAdvance(TokenKind kind, out Token token, bool linesensitive = false)
        {
            token = Current;
            var match = MatchAdvance(kind, linesensitive);
            return match;
        }

        private Token ExpectConstant(string error)
        {
            var match = MatchConstantAdvance();

            if (match)
                return Back;

            Report(error);

            return new();
        }

        private ParameterNode ExpectParameter(int count)
        {
            if (count > 0)
                Expect(TokenKind.Comma);

            var isStatic = MatchAdvance(TokenKind.KeyStatic);
            var name = Expect(TokenKind.Identifier);
            var result = new ParameterNode(null, name.Value, CreateBadNode(), isStatic, name.Position);

            if (MatchAdvance(TokenKind.Colon))
                result.Type = ExpectType();

            return result;
        }

        private bool MatchValue()
        {
            return
                MatchAdvance(TokenKind.Identifier)
                || MatchConstantAdvance();
        }

        private bool MatchConstantAdvance()
        {
            return
                MatchAdvance(TokenKind.ConstantChar)
                || MatchAdvance(TokenKind.ConstantDigit)
                || MatchAdvance(TokenKind.ConstantFloatDigit)
                || MatchAdvance(TokenKind.ConstantString)
                || MatchAdvance(TokenKind.ConstantBoolean);
        }

        private BadNode CreateBadNode()
        {
            return new(Back.Position);
        }

        private bool MatchInParExpression(out INode e)
        {
            e = CreateBadNode();

            var start = Current.Position;
            if (!MatchAdvance(TokenKind.OpenPar))
                return false;

            e = ExpectExpression(true, true, false, true, TokenKind.ClosePar);

            var end = Back.Position;

            e.Position = GetModulePositionRange(start, end);
            return true;
        }

        private bool IsInvalidNode(INode node)
        {
            return node is BadNode or null;
        }

        private void CollectPossibleArrayAccessNode(ref INode e)
        {
            if (IsInvalidNode(e))
                return;

            while (MatchAdvance(TokenKind.OpenBracket))
            {
                e = new ArraySelectElemNode()
                {
                    IndexExpression = ExpectExpression(TokenKind.CloseBracket),
                    Left = e,
                    Position = GetModulePositionRange(e.Position, Back.Position)
                };
            }
        }

        private bool MatchMember(out INode name)
        {
            if (MatchValue())
                name = Back;
            else if (!MatchInParExpression(out name))
                return false;
            
            CollectPossibleArrayAccessNode(ref name);

            while (MatchAdvance(TokenKind.Dot))
            {
                var id = Expect(TokenKind.Identifier);

                name = new MemberNode()
                {
                    Base = name,
                    Member = id,
                    Position = new(name.Position.Source, name.Position.Position.Start.Value..id.Position.Position.End.Value)
                };

                CollectPossibleArrayAccessNode(ref name);
            }

            return true;
        }

        private NodeBuilder CollectParameters()
        {
            var parameters = new NodeBuilder();
            var start = Expect(TokenKind.OpenPar).Position;

            if (!MatchAdvance(TokenKind.ClosePar))
                while (Back.Kind is not TokenKind.ClosePar)
                    parameters.Add(ExpectExpression(TokenKind.Comma, TokenKind.ClosePar));

            parameters.Position = GetModulePositionRange(start, Back.Position);
            return parameters;
        }

        private bool CollectBuiltInSymbol()
        {
            return MatchAdvance(TokenKind.Negation, true);
        }

        private bool MatchCallStatement(out INode e, INode name)
        {
            e = CreateBadNode();

            if (!Match(TokenKind.OpenPar, true) && !Match(TokenKind.Negation, true))
                return false;

            var builtin = CollectBuiltInSymbol();
            var parameters = CollectParameters();

            e = new CallNode
            {
                IsBuiltIn = builtin,
                Name = name,
                Parameters = parameters,
                Position = GetModulePositionRange(name.Position, Back.Position)
            };

            return true;
        }

        private void CollectCatchExpression(ref INode e)
        {
            if (!MatchAdvance(TokenKind.KeyCatch))
                return;
            var match = MatchAdvance(TokenKind.Identifier, out var error);

            e = new CatchExpressionNode()
            {
                Expression = e,
                OutError = match ? new Token?(error) : null,
                Position = Back.Position,
                Body = ExpectBlock()
            };
        }

        private void CollectDotExpression(ref INode e)
        {
            var name = Expect(TokenKind.Identifier);

            var builtin = CollectBuiltInSymbol();

            e =
                Match(TokenKind.OpenPar, true) ?
                    e = new CallNode
                    {
                        Parameters = CollectParameters(),
                        IsBuiltIn = builtin,
                        Position = name.Position,
                        Name = new MemberNode
                        {
                            Base = e,
                            Member = name,
                            Position = new(e.Position.Source, e.Position.Position.Start..name.Position.Position.End)
                        },
                    }
                    :
                    new MemberNode
                    {
                        Base = e,
                        Member = name,
                        Position = new(e.Position.Source, e.Position.Position.Start..name.Position.Position.End)
                    };
        }

        private bool MatchPrefixOperator(out Token prefix)
        {
            return
                MatchAdvance(TokenKind.Minus, out prefix)
                || MatchAdvance(TokenKind.Plus, out prefix)
                || MatchAdvance(TokenKind.Negation, out prefix)
                || MatchAdvance(TokenKind.Apersand, out prefix)
                || MatchAdvance(TokenKind.Star, out prefix)
                || MatchAdvance(TokenKind.OperatorIncrement, out prefix)
                || MatchAdvance(TokenKind.OperatorDecrement, out prefix);
        }

        private bool MatchTerm(out INode e, bool allowNullExpression)
        {
            if (Match(TokenKind.OpenBrace))
                return CollectBlockExpression(out e);
            if (MatchAdvance(TokenKind.KeyTry))
                return CollectTryExpression(out e);
            if (MatchPrefixOperator(out var prefixOP))
                return CollectPrefixOperator(out e, allowNullExpression, prefixOP);

            // arr[]
            // base.member
            // base.member()
            // base()
            // base.method().other_method()

            if (!MatchMember(out e))
            {
                if (MatchAdvance(TokenKind.KeyNew, out var token))
                    e = CollectNodeNew(token.Position);
                else if (ConditionDefinition(out e)
                    || FunctionDefinition(out e)
                    || StructDefinition(out e)
                    || EnumDefinition(out e))
                    return true;
            }

            if (!allowNullExpression && IsInvalidNode(e))
                return false;

            CollectPossibleArrayAccessNode(ref e);

            if (MatchCallStatement(out var call, e))
                e = call;

            CollectPossibleArrayAccessNode(ref e);

            CollectPossibleDotExpression(ref e);

            CollectPostfixOperator(ref e);

            CollectAsExpression(ref e);

            return true;
        }

        private bool CollectBlockExpression(out INode e)
        {
            e = ExpectBlock();
            return true;
        }

        private bool CollectTryExpression(out INode e)
        {
            if (!MatchTerm(out e, false) || e is not CallNode)
            {
                Report(Back.Position, "Expected call expression after 'try'");
                return false;
            }

            e = new TryExpressionNode()
            {
                Expression = e,
                Position = Back.Position
            };

            return true;
        }

        private bool CollectPrefixOperator(out INode e, bool allowNullExpression, Token prefixOP)
        {
            if (!MatchTerm(out e, allowNullExpression))
            {
                Report("Unexpected prefix operator");
                return false;
            }

            e = new PrefixOperator()
            {
                Expression = e,
                Prefix = prefixOP,
                Position = GetModulePositionRange(prefixOP.Position, e.Position)
            };

            return true;
        }

        private void CollectPostfixOperator(ref INode e)
        {
            if (MatchAdvance(TokenKind.OperatorIncrement, true) || MatchAdvance(TokenKind.OperatorDecrement, true))
                e = new PostfixOperator() { Expression = e, Position = Back.Position, Postfix = Back };
        }

        private void CollectPossibleDotExpression(ref INode e)
        {
            while (MatchAdvance(TokenKind.Dot))
                CollectDotExpression(ref e);
        }

        private INode ExpectFactor(bool allowNullExpression)
        {
            if (!MatchFactor(out var e, allowNullExpression))
                ParseError(UnexpectedToken);

            return e;
        }

        private bool MatchFactorOps()
        {
            return
                MatchAdvance(TokenKind.Star, true) ||
                MatchAdvance(TokenKind.Slash, true) ||
                MatchAdvance(TokenKind.RangeDots, true);
        }

        private INode ExpectExpression(params TokenKind[] end)
        {
            return ExpectExpression(true, true, false, true, end);
        }

        private FieldAssignmentNode ExpectFieldAssign()
        {
            if (!MatchAdvance(TokenKind.Identifier))
            {
                Report("Expected field assignment");
                return null;
            }

            var name = Back;
            Expect(TokenKind.Colon);

            var expression = ExpectExpression(TokenKind.Comma, TokenKind.CloseBrace);
            CurrentIndex--;

            return new FieldAssignmentNode() { Name = name.Value.ToString(), Body = expression, Position = name.Position };
        }

        private INode ExpectTerm(bool allowNullExpression)
        {
            if (!MatchTerm(out var e, allowNullExpression))
                Report(UnexpectedToken);

            return e;
        }
        
        private static ModulePosition GetModulePositionRange(ModulePosition left, ModulePosition right)
        {
            return new(left.Source, new(left.Position.Start, right.Position.End));
        }

        private bool MatchFactor(out INode e, bool allowNullExpression)
        {
            if (!MatchTerm(out e, allowNullExpression))
                return false;

            if (!MatchFactorOps())
                return true;
                
            var op = Back;
            var right = ExpectTerm(allowNullExpression);

            do
            {
                e = new BinaryExpressionNode()
                {
                    Left = e,
                    Right = right,
                    Operator = op,
                    Position = GetModulePositionRange(e.Position, right.Position)
                };

                if (MatchFactorOps())
                    op = Back;
                else
                    break;
            } while (MatchTerm(out right, allowNullExpression));

            return true;
        }

        private bool MatchBooleanOperator(out Token op)
        {
            return
                MatchAdvance(TokenKind.BooleanEQ, out op) ||
                MatchAdvance(TokenKind.BooleanNEQ, out op) ||
                MatchAdvance(TokenKind.BooleanGreater, out op) ||
                MatchAdvance(TokenKind.BooleanLess, out op) ||
                MatchAdvance(TokenKind.BooleanGEQ, out op) ||
                MatchAdvance(TokenKind.BooleanLEQ, out op) ||
                MatchAdvance(TokenKind.KeyIn, out op);
        }

        private INode CollectNodeNew(ModulePosition newposition)
        {
            // could be type inferred
            var name = ExpectTypeOr(TokenKind.OpenBrace);

            var allocation = new TypeAllocationNode { Name = name, Position = newposition };

            Expect(TokenKind.OpenBrace, UnexpectedToken);

            do
            {
                if (Match(TokenKind.CloseBrace))
                    break;

                var assignment = ExpectFieldAssign();
                allocation.Body.Add(assignment);
            } while (MatchAdvance(TokenKind.Comma));

            Expect(TokenKind.CloseBrace, UnexpectedToken);

            return allocation;
        }

        private INode ExpectExpressionOr(TokenKind kind)
        {
            if (Match(kind))
                return CreateBadNode();
            
            return ExpectExpression(kind);
        }

        private INode ExpectTypeOr(TokenKind kind)
        {
            if (Match(kind))
                return CreateBadNode();
            
            return ExpectType(kind);
        }

        private bool MatchAndOrOperator()
        {
            return
                MatchAdvance(TokenKind.BooleanOR, true)
                || MatchAdvance(TokenKind.BooleanAND, true);
        }

        private bool MatchPlusMinus()
        {
            return
                MatchAdvance(TokenKind.Plus, true) ||
                MatchAdvance(TokenKind.Minus, true);
        }

        private INode ExpectType(params TokenKind[] end)
        {
            return ExpectExpression(false, false, false, false, end);
        }

        private INode ExpectExpression(
            bool allowBoolOP,
            bool allowLogicOP,
            bool allowNullExpression,
            bool allowAssignment = false,
            params TokenKind[] end)
        {
            var isFirstCallToExpectExpression = allowBoolOP & allowLogicOP;

            if (MatchFactor(out var e, allowNullExpression) && MatchPlusMinus())
                CollectPlusMinusExpression(allowNullExpression, ref e);

            if (IsInvalidNode(e) && !allowNullExpression)
            {
                Report($"Expected expression, found '{Current.Value}'");
                Advance(); // skipping bad token
            }

            CollectBooleanBinaryExpressions(allowBoolOP, end, ref e);
            CollectCatchExpression(ref e);
            CollectLogicBinaryExpressions(allowLogicOP, end, ref e);

            if (allowAssignment)
                CollectAssignmentNode(ref e);

            if (isFirstCallToExpectExpression)
                ExpectMultiple($"Invalid token here, missing one of '{TokenKindsToString(end)}'?", end);

            return e;
        }

        private void Advance()
        {
            CurrentIndex++;
        }

        private void CollectAssignmentNode(ref INode e)
        {
            if (MatchAssigmentOperators(out var @operator))
                e = new AssignmentNode
                {
                    Name = e,
                    Operator = @operator,
                    Position = @operator.Position,
                    Body = ExpectExpression()
                };
        }

        private void CollectLogicBinaryExpressions(bool allowLogicOP, TokenKind[] end, ref INode e)
        {
            while (allowLogicOP && MatchAndOrOperator())
            {
                var op = Back;
                var right = ExpectExpression(
                    allowBoolOP: true,
                    allowLogicOP: false,
                    allowNullExpression: false,
                    end: end);
                    
                e = new BinaryExpressionNode
                {
                    Operator = op,
                    Left = e,
                    Right = right,
                    Position = GetModulePositionRange(e.Position, right.Position)
                };
            }
        }

        private void CollectBooleanBinaryExpressions(bool allowBoolOP, TokenKind[] end, ref INode e)
        {
            while (allowBoolOP && MatchBooleanOperator(out var boolOP))
                e = new BinaryExpressionNode
                {
                    Operator = boolOP,
                    Left = e,
                    Right = ExpectExpression(
                        allowBoolOP: false,
                        allowLogicOP: false,
                        allowNullExpression: false,
                        end: end),
                    Position = GetModulePositionRange(e.Position, e.Position)
                };
        }

        private void CollectPlusMinusExpression(bool allowNullExpression, ref INode e)
        {
            var op = Back;
            var right = ExpectFactor(allowNullExpression);

            do
            {
                e = new BinaryExpressionNode
                {
                    Operator = op,
                    Left = e,
                    Right = right,
                    Position = GetModulePositionRange(e.Position, right.Position)
                };

                if (MatchPlusMinus())
                    op = Back;
                else
                    break;
            } while (MatchFactor(out right, allowNullExpression));
        }

        private void CollectAsExpression(ref INode e)
        {
            if (MatchAdvance(TokenKind.KeyAs, out var token))
                e = new CastExpressionNode
                {
                    Expression = e,
                    Type = ExpectType(),
                    Position = token.Position
                };
        }

        private INode ExpectVariableType()
        {
            return MatchAdvance(TokenKind.Colon) ? ExpectType() : CreateBadNode();
        }

        private bool VariableDefinition(out INode statement)
        {
            statement = CreateBadNode();

            var isMutable = false;
            if (MatchAdvance(TokenKind.KeyLet, out var token))
                isMutable = MatchAdvance(TokenKind.KeyMut);
            else if (!MatchAdvance(TokenKind.KeyConst))
                return false;

            var modifiers = GetModifiers();
            var pragmas = GetPramas();
            var name = Expect(TokenKind.Identifier);
            var type = ExpectVariableType();
            var body = MatchAdvance(TokenKind.Equal) ? ExpectExpression() : CreateBadNode();

            statement = new VariableNode
            {
                Modifiers = modifiers,
                Pragmas = pragmas,
                IsMutable = isMutable,
                Body = body,
                Name = name.Value.ToString(),
                Position = name.Position,
                Type = type,
                IsConst = token.Kind is TokenKind.KeyConst
            };

            return true;
        }

        private bool ReturnDeclaration(out INode statement)
        {
            statement = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyReturn, out var key))
                return false;

            statement = new ReturnNode
            {
                Position = key.Position,
                Body =
                    !Match(TokenKind.CloseBrace) && CurrentIsOnTheSameLine() ?
                        ExpectExpression() :
                        CreateBadNode()
            };

            return true;
        }

        private bool CurrentIsOnTheSameLine()
        {
            return !Current.IsOnNewLine;
        }

        private bool MatchAssigmentOperators(out Token op)
        {
            return
                MatchAdvance(TokenKind.Equal, out op)
                || MatchAdvance(TokenKind.AddAssignment, out op)
                || MatchAdvance(TokenKind.SubAssignment, out op)
                || MatchAdvance(TokenKind.MulAssignment, out op)
                || MatchAdvance(TokenKind.DivAssignment, out op);
        }

        private bool CollectMatchExpression(out INode statement, ModulePosition position)
        {
            var matchexpr = new SwitchCaseNode() { Position = position, Expression = ExpectExpression(TokenKind.OpenBrace) };

            while (!MatchAdvance(TokenKind.CloseBrace))
            {
                var expression = (INode)CreateBadNode();
                var pos = Current.Position;

                if (!MatchAdvance(TokenKind.KeyElse))
                {
                    expression = ExpectExpression(end: TokenKind.OpenBrace);
                    pos = expression.Position;
                    CurrentIndex--;
                }

                matchexpr.Body.Add(new CaseNode()
                {
                    Expression = expression,
                    Body = ExpectBlock(),
                    Position = pos,
                });
            }

            statement = matchexpr;
            return true;
        }

        private bool ConditionDefinition(out INode statement, bool isFirstCondition = true)
        {
            statement = CreateBadNode();

            if (isFirstCondition && MatchAdvance(TokenKind.KeySwitch, out var matchtoken))
                return CollectMatchExpression(out statement, matchtoken.Position);

            if (!MatchAdvance(TokenKind.KeyIf, out var key) &&
                !MatchAdvance(TokenKind.KeyElif, out key) &&
                !MatchAdvance(TokenKind.KeyElse, out key) &&
                !MatchAdvance(TokenKind.KeyWhile, out key))
                return false;

            if (isFirstCondition && key.Kind != TokenKind.KeyIf && key.Kind != TokenKind.KeyWhile)
            {
                CurrentIndex--;
                Report("The 'elif' and 'else' conditions shall be referenced to an 'if' block");
                return false;
            }

            INode expression = CreateBadNode();
            if (key.Kind != TokenKind.KeyElse)
            {
                expression = ExpectExpression(TokenKind.OpenBrace);
                CurrentIndex--;
            }

            var body = ExpectBlock();

            INode elif = null;
            if (key.Kind != TokenKind.KeyWhile && key.Kind != TokenKind.KeyElse && (Match(TokenKind.KeyElif) || Match(TokenKind.KeyElse)))
                ConditionDefinition(out elif, false);

            statement = new ConditionalNode() { Position = key.Position, Expression = expression, Kind = key.Kind, Body = body, ElseNode = (ConditionalNode)elif };

            return true;
        }

        private bool ForLoopDefinition(out INode statement)
        {
            statement = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyFor, out var key))
                return false;

            var iterator = Expect(TokenKind.Identifier);
            Expect(TokenKind.KeyIn);
            var expression = ExpectExpression();

            statement = new ForLoopNode()
            {
                Body = ExpectBlock(),
                Iterator = iterator,
                Expression = expression,
                Position = key.Position
            };

            return true;
        }

        private bool LoopManagerDefintion(out INode statement)
        {
            statement = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyContinue) &&
                !MatchAdvance(TokenKind.KeyBreak))
                return false;

            statement = new LoopManagementNode() { Kind = Back.Kind, Position = Back.Position };

            return true;
        }

        private INode ExpectStatement(bool isfirst, bool allowNull = false)
        {
            var multipleStatementsOnTheSameLine = !isfirst && !Current.IsOnNewLine;

            if (!VariableDefinition(out var statement) && // var x = value;
                !ReturnDeclaration(out statement) && // return value;
                !ForLoopDefinition(out statement) && // for x: type to, in value {}
                !LoopManagerDefintion(out statement)) // continue, break
                statement =
                    ExpectExpression(
                        allowBoolOP: true,
                        allowLogicOP: true,
                        allowNullExpression: allowNull,
                        allowAssignment: true);

            if (multipleStatementsOnTheSameLine)
                Tower.Warn(statement.Position, "Maintain statements on different lines");

            return statement;
        }

        private BlockNode ExpectBlock()
        {
            var start = Expect(TokenKind.OpenBrace).Position;
            var block = new BlockNode();
            var i = 0;

            while (!Match(TokenKind.CloseBrace))
            {
                var statement = ExpectStatement(i++ == 0);
                if (statement is not BadNode)
                    block.Statements.Add(statement);
            }

            var end = Expect(TokenKind.CloseBrace).Position;

            block.Position = GetModulePositionRange(start, end);
            return block;
        }

        private ParameterNode[] ExpectParameterListDeclaration(ModulePosition position, out INode type)
        {
            type = Token.NewInfo(TokenKind.Identifier, "void", position);

            if (Match(TokenKind.OpenBrace))
                return Array.Empty<ParameterNode>();
            
            Expect(TokenKind.OpenPar);

            var parameters = new List<ParameterNode>();
            var count = 0;

            while (!MatchAdvance(TokenKind.ClosePar))
                parameters.Add(ExpectParameter(count++));

            if (MatchAdvance(TokenKind.Colon))
                type = ExpectType();

            var result = parameters.ToArray();
            FixImplicitlyTypedParameters(ref result);

            return result;
        }

        private void FixImplicitlyTypedParameters(ref ParameterNode[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
                FixParameterTypeWhetherImplicit(parameters, parameters[i], ref i);
        }

        private void FixParameterTypeWhetherImplicit(ParameterNode[] parameters, ParameterNode parameter, ref int i)
        {
            if (parameter.Type is not null)
                return;

            if (!CouldFindNextExplicitType(parameters, i, out var index))
            {
                Report(parameter.Position, $"Unable to infer type");
                parameters[i].Type = CreateBadNode();
            }
            else
                InferTypeInImplicitlyTypedParameters(parameters, index, ref i);
        }

        private static bool CouldFindNextExplicitType(ParameterNode[] parameters, int i, out int index)
        {
            for (index = i; index < parameters.Length; index++)
                if (parameters[index].Type is not null)
                    return true;

            return false;
        }

        private static void InferTypeInImplicitlyTypedParameters(ParameterNode[] parameters, int index, ref int i)
        {
            var isStatic = parameters[index].IsStatic;
            var type = parameters[index].Type;
            var parametersCount = index - i;

            while (parametersCount-- >= 0)
            {
                parameters[i].IsStatic = isStatic;
                parameters[i].Type = type;
                i++;
            }
        }

        private ImmutableArray<TokenKind> GetModifiers()
        {
            if (_modifiers.Count == 0)
                return ImmutableArray.Create<TokenKind>();

            var old = _modifiers.ToImmutable();

            _modifiers.Clear();

            return old;
        }

        private Pragmas GetPramas()
        {
            var result = _pragmas;
            _pragmas = null;
            return result ?? new();
        }

        private bool FunctionDefinition(out INode node)
        {
            node = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyFunc, out var key)) // <func>
                return false;

            var parameters = ExpectParameterListDeclaration(key.Position, out var type); // func name<(..)>

            var prototype = new FunctionNode
            {
                ParameterList = parameters,
                Type = type,
                Position = key.Position
            };

            if (Match(TokenKind.OpenBrace)) // function definition
                prototype.Body = ExpectBlock();

            node = prototype;
            return true;
        }

        private FieldNode ExpectFieldDefinition()
        {
            var modifiers = GetModifiers();
            var pragmas = GetPramas();
            var name = Expect(TokenKind.Identifier); // <field>

            if (!MatchAdvance(TokenKind.Colon))
            {
                Report(UnexpectedToken);
                return new FieldNode { Name = "", Type = Token.NewInfo(TokenKind.Identifier, "void", name.Position) };
            }

            var type = ExpectType(); // field: <error>
            EatComma();

            return new FieldNode
            {
                Pragmas = pragmas,
                Modifiers = modifiers,
                Name = name.Value.ToString(),
                Type = type,
                Position = name.Position
            };
        }

        /* private void ExpectVariantDefinition(out INode node, Token name, Pragmas pragmas, TokenKind modifier)
        {
            node = CreateBadNode();
            var variant = new VariantNode()
            {
                Pragmas = pragmas,
                Modifier = modifier,
                Name = name.Value,
                Position = name.Position
            };

            Expect(TokenKind.OpenPar);

            if (MatchAdvance(TokenKind.ClosePar))
                return;

            do
                variant.Body.Add(ExpectType());
            while (MatchAdvance(TokenKind.BooleanOR));

            Expect(TokenKind.ClosePar);

            node = variant;
        } */

        /// <summary>
        /// search for a struct definition
        /// </summary>
        private bool StructDefinition(out INode node)
        {
            node = CreateBadNode();

            // returns if does not match a type keyword
            if (!MatchAdvance(TokenKind.KeyStruct, out var key))
                return false;

            node = new StructureNode
            {
                Position = key.Position
            };

            CollectStructBody(node as StructureNode);

            return true;
        }

        private void CollectStructBody(StructureNode statement)
        {
            Expect(TokenKind.OpenBrace, UnexpectedToken);

            while (!MatchAdvance(TokenKind.CloseBrace))
            {
                CollectModifiers();
                CollectPragmas();

                if (VariableDefinition(out var node))
                    statement.BodyMembers.Add(node as VariableNode);
                else
                    statement.BodyFields.Add(ExpectFieldDefinition());
            }
        }

        private void EatComma()
        {
            MatchAdvance(TokenKind.Comma);
        }

        private void Report(string error)
        {
            Tower.Report(Current.Position, error);
        }

        private void Report(ModulePosition position, string error)
        {
            Tower.Report(position, error);
        }

        private bool EnumDefinition(out INode node)
        {
            node = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyEnum, out var key))
                return false;

            var basetype = GetEnumBaseType();
            var statement = new EnumNode
            {
                BaseType = basetype,
                Position = key.Position
            };

            // enum body
            CollectEnumBody(ref statement);

            node = statement;

            return true;
        }

        private void CollectEnumBody(ref EnumNode statement)
        {
            Expect(TokenKind.OpenBrace);

            do
            {
                if (Match(TokenKind.CloseBrace))
                    break;

                statement.Body.Add(ExpectEnumMemberNode());
            } while (MatchAdvance(TokenKind.Comma));

            Expect(TokenKind.CloseBrace);
        }

        private EnumMemberNode ExpectEnumMemberNode()
        {
            var name = Expect(TokenKind.Identifier);
            var value =
                MatchAdvance(TokenKind.Colon) ?
                    ExpectExpression() :
                    CreateBadNode();

            return new EnumMemberNode
            {
                Name = name.Value,
                Value = value,
                Position = name.Position
            };
        }

        private INode GetEnumBaseType()
        {
            return Match(TokenKind.OpenBrace) ? CreateBadNode() : ExpectType();
        }

        private void CollectPragmas()
        {
            if (!MatchAdvance(TokenKind.OpenBracket))
                return;

            _pragmas = new();

            do
            {
                var name = Expect(TokenKind.Identifier);
                var value = MatchAdvance(TokenKind.Colon) ?
                    ExpectConstant("Non-constant expressions not allowed in pragmas") : Token.NewInfo(TokenKind.ConstantBoolean, "true");

                _pragmas.SetPragma(name.Value, value, Tower, name.Position);
            } while (MatchAdvance(TokenKind.Comma));

            Expect(TokenKind.CloseBracket);
        }

        private void CollectModifiers()
        {
            while (
                MatchAdvance(TokenKind.KeyPub)
                || MatchAdvance(TokenKind.KeyPriv)
                || MatchAdvance(TokenKind.KeyStatic))
            {
                if (ModifierAlreadyCollected(Back.Kind))
                    Report(Back.Position, $"Modifier '{Back.Kind.GetDescription()}' is declared multiple times");
                else
                    _modifiers.Add(Back.Kind);
            }
        }

        private bool ModifierAlreadyCollected(TokenKind kind)
        {
            for (int i = 0; i < _modifiers.Count; i++)
                if (_modifiers[i] == kind)
                    return true;

            return false;
        }

        /// <summary>
        /// expects at least one member
        /// </summary>
        private List<VariableNode> ExpectNamespaceMembers(TokenKind end = TokenKind.EOF)
        {
            var nodes = new List<VariableNode>();

            // while the current token is not end
            while (!Match(end))
            {
                // collecting pragmas for first two statements (function and type)
                CollectModifiers();
                CollectPragmas();

                var statement = ExpectGlobalVariableDefinition();

                // adds the statement to the members
                nodes.Add(statement);
            }

            return nodes;
        }

        private VariableNode ExpectGlobalVariableDefinition()
        {
            if (!VariableDefinition(out var globalStatement))
            {
                Report("Expected variable definition");
                Advance();
            }
            
            return globalStatement as VariableNode;
        }

        /// <summary>
        /// generates the ast from a tokens stream
        /// </summary>
        public NamespaceNode Parse()
        {
            Reset();

            // to avoid bugs
            if (Match(TokenKind.EOF))
                return Head;

            // search for members
            Head.Members = ExpectNamespaceMembers();
            
            if (_modifiers.Count > 0
                || _pragmas is not null)
                Report("Expected variable declaration");

            // breaking the compiler workflow if diagnostic is bad
            // Tower.CheckDiagnostic();

            return Head;
        }

        private void Reset()
        {
            _modifiers.Clear();
            _pragmas = null;
            CurrentIndex = 0;
            Head = new NamespaceNode();
        }
    }
}