using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser.NodeKinds;
using Mug.Models.Parser.NodeKinds.Directives;
using Mug.Models.Parser.NodeKinds.Statements;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mug.Models.Parser
{
    public class MugParser
    {
        public NamespaceNode Module = new();
        public MugLexer Lexer;
        private int _currentIndex = 0;
        private Pragmas _pragmas;
        private TokenKind _modifier = TokenKind.Bad;

        private void ParseError(string error)
        {
            if (Match(TokenKind.EOF))
                ParseErrorEOF(error);

            Lexer.Throw(Current, error);
        }

        private void ParseError(ModulePosition position, string error)
        {
            if (Match(TokenKind.EOF))
                ParseErrorEOF(error);

            Lexer.Throw(position, error);
        }

        private void ParseErrorEOF(string error)
        {
            Lexer.Throw(Back, $"Unexpected <EOF>{(error != "" ? $": {error}" : "")}");
        }

        public MugParser(string moduleName, string source)
        {
            Lexer = new(moduleName, source);
        }

        public MugParser(MugLexer lexer)
        {
            Lexer = lexer;
        }

        private Token Current
        {
            get
            {
                if (_currentIndex >= Lexer.TokenCollection.Count)
                    ParseErrorEOF("");

                return Lexer.TokenCollection[_currentIndex];
            }
        }

        private Token Back
        {
            get
            {
                if (_currentIndex - 1 >= 0)
                    return Lexer.TokenCollection[_currentIndex - 1];

                return new Token();
            }
        }

        private string UnexpectedToken => $"Unexpected token '{Current.Value}'";

        private string TokenKindsToString(TokenKind[] kinds)
        {
            StringBuilder result = new();

            for (int i = 0; i < kinds.Length; i++)
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

                for (int i = 0; i < kinds.Length; i++)
                    if (Match(kinds[i]))
                    {
                        _currentIndex++;
                        return Back;
                    }

                if (error == "")
                    Report($"Expected '{TokenKindsToString(kinds)}', found '{Current.Value}'");
                else
                    Report(error);
            } 

            return Token.NewInfo(kinds.FirstOrDefault(), "");
        }

        private Token Expect(string error, TokenKind kind)
        {
            if (!Match(kind))
            {
                if (error == "")
                    Report($"Expected '{kind.GetDescription()}', found '{Current.Value}'");
                else
                    Report(error);
            }

            _currentIndex++;
            return Back;
        }

        private bool Match(TokenKind kind, bool linesensitive = false)
        {
            if (!linesensitive && Current.Kind == TokenKind.EOL)
            {
                if (_currentIndex < Lexer.TokenCollection.Count && Lexer.TokenCollection[_currentIndex+1].Kind == kind)
                    _currentIndex++;
            }

            if (!linesensitive && Current.Kind == TokenKind.EOL)
                return false;

            return Current.Kind == kind;
        }

        private bool MatchAdvance(TokenKind kind, bool linesensitive = false)
        {
            var expect = Match(kind, linesensitive);

            if (expect)
                _currentIndex++;

            return expect;
        }

        private MugType ExpectType(bool allowEnumError = false)
        {
            if (MatchAdvance(TokenKind.BooleanAND, out var token))
                return new MugType(token.Position, TypeKind.Reference, ExpectType(false));
            if (MatchAdvance(TokenKind.OpenBracket, out token))
            {
                var type = ExpectType();
                Expect("An array type definition must end by ']'", TokenKind.CloseBracket);
                return new MugType(new(token.Position.Lexer, token.Position.Position.Start..Back.Position.Position.End), TypeKind.Array, type);
            }
            else if (MatchAdvance(TokenKind.Star, out token))
            {
                var type = ExpectType();
                return new MugType(new(token.Position.Lexer, token.Position.Position.Start..type.Position.Position.End), TypeKind.Pointer, type);
            }

            var find = ExpectBaseType();

            // struct generics
            if (MatchAdvance(TokenKind.BooleanLess))
            {
                if (find.Kind != TypeKind.DefinedType)
                {
                    _currentIndex -= 2;
                    ParseError($"Generic parameters cannot be passed to type '{find}'");
                }

                var genericTypes = new List<MugType>();

                do
                    genericTypes.Add(ExpectType());
                while (MatchAdvance(TokenKind.Comma));

                Expect("", TokenKind.BooleanGreater);

                find = new MugType(new(find.Position.Lexer, find.Position.Position.Start..Back.Position.Position.End), TypeKind.GenericDefinedType, (find, genericTypes));
            }

            if (allowEnumError && MatchAdvance(TokenKind.Negation))
                find = new MugType(Back.Position, TypeKind.EnumError, (find, ExpectType()));

            return find;
        }

        private bool MatchType(out MugType type)
        {
            type = null;
            MugType t = null;

            if (!Match(TokenKind.OpenBracket) && !Match(TokenKind.Star) && !MatchBaseType(out t))
                return false;

            if (t is not null)
                _currentIndex--;

            type = ExpectType();
            return true;
        }

        private bool MatchAdvance(TokenKind kind, out Token token, bool linesensitive = false)
        {
            var match = MatchAdvance(kind, linesensitive);
            token = Back;
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

        private ParameterNode ExpectParameter(bool isFirst)
        {
            if (!isFirst)
                Expect("", TokenKind.Comma);

            var name = Expect("Expected parameter's name", TokenKind.Identifier);

            Expect("Expected parameter's type", TokenKind.Colon);

            var type = ExpectType();
            var defaultvalue = new Token();

            if (MatchAdvance(TokenKind.Equal))
                defaultvalue = ExpectConstant("Expected constant expression as default parameter value");

            ExpectMultiple("", TokenKind.Comma, TokenKind.ClosePar);
            _currentIndex--;

            return new ParameterNode(type, name.Value, defaultvalue, name.Position);
        }

        private OperatorKind ToOperatorKind(TokenKind op)
        {
            return op switch
            {
                TokenKind.Plus => OperatorKind.Sum,
                TokenKind.Minus => OperatorKind.Subtract,
                TokenKind.Star => OperatorKind.Multiply,
                TokenKind.Slash => OperatorKind.Divide,
                TokenKind.RangeDots => OperatorKind.Range,
                TokenKind.BooleanEQ => OperatorKind.CompareEQ,
                TokenKind.BooleanNEQ => OperatorKind.CompareNEQ,
                TokenKind.BooleanGreater => OperatorKind.CompareGreater,
                TokenKind.BooleanGEQ => OperatorKind.CompareGEQ,
                TokenKind.BooleanLess => OperatorKind.CompareLess,
                TokenKind.BooleanLEQ => OperatorKind.CompareLEQ,
                TokenKind.BooleanAND => OperatorKind.And,
                TokenKind.BooleanOR => OperatorKind.Or,
                _ => throw new Exception($"Unable to perform cast from TokenKind('{op}') to OperatorKind, if you see this error please open an issue on github")
            };
        }

        private MugType ExpectBaseType()
        {
            if (!MatchBaseType(out var type))
                ParseError("Expected a type, but found '" + Current.Value + "'");

            return type;
        }

        private bool MatchBaseType(out MugType type)
        {
            type = null;
            
            if (!MatchPrimitiveType(out var token) && !MatchAdvance(TokenKind.Identifier, out token))
                return false;

            type = MugType.FromToken(token);

            return true;
        }

        private bool MatchPrimitiveType(out Token type)
        {
            return
                MatchAdvance(TokenKind.KeyTi32, out type) ||
                MatchAdvance(TokenKind.KeyTVoid, out type) ||
                MatchAdvance(TokenKind.KeyTbool, out type) ||
                MatchAdvance(TokenKind.KeyTchr, out type) ||
                MatchAdvance(TokenKind.KeyTf32, out type) ||
                MatchAdvance(TokenKind.KeyTf64, out type) ||
                MatchAdvance(TokenKind.KeyTf128, out type) ||
                MatchAdvance(TokenKind.KeyTi64, out type) ||
                MatchAdvance(TokenKind.KeyTu32, out type) ||
                MatchAdvance(TokenKind.KeyTu8, out type) ||
                MatchAdvance(TokenKind.KeyTu64, out type) ||
                MatchAdvance(TokenKind.KeyTstr, out type) ||
                MatchAdvance(TokenKind.KeyTunknown, out type);
        }

        private bool MatchValue()
        {
            return
                MatchAdvance(TokenKind.Identifier) ||
                MatchConstantAdvance();
        }

        private bool MatchConstantAdvance()
        {
            var current = Current.Kind;
            var constant =
                current == TokenKind.ConstantChar ||
                current == TokenKind.ConstantDigit ||
                current == TokenKind.ConstantFloatDigit ||
                current == TokenKind.ConstantString ||
                current == TokenKind.ConstantBoolean;

            if (constant)
                _currentIndex++;

            return constant;
        }

        private bool MatchInParExpression(out INode e)
        {
            e = null;

            if (!MatchAdvance(TokenKind.OpenPar, out var token))
                return false;

            e = ExpectExpression(true, end: TokenKind.ClosePar);

            return true;
        }

        private void CollectPossibleArrayAccessNode(ref INode e)
        {
            while (MatchAdvance(TokenKind.OpenBracket, out var token))
            {
                e = new ArraySelectElemNode()
                {
                    IndexExpression = ExpectExpression(end: TokenKind.CloseBracket),
                    Left = e,
                    Position = new(token.Position.Lexer, token.Position.Position.Start..Back.Position.Position.End)
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
                var id = Expect("Expected member after '.'", TokenKind.Identifier);

                name = new MemberNode() { Base = name, Member = id, Position = new(name.Position.Lexer, name.Position.Position.Start.Value..id.Position.Position.End.Value) };

                CollectPossibleArrayAccessNode(ref name);
            }

            return true;
        }

        private void CollectParameters(ref NodeBuilder parameters)
        {
            var lastiscomma = false;
            while (!MatchAdvance(TokenKind.ClosePar))
            {
                parameters.Add(ExpectExpression(end: new[] { TokenKind.Comma, TokenKind.ClosePar}));

                lastiscomma = Back.Kind == TokenKind.Comma;
                if (Back.Kind == TokenKind.ClosePar)
                {
                    lastiscomma = false;
                    _currentIndex--;
                }
            }

            if (lastiscomma)
                Report(Back.Position, "Expected parameter's expression");
        }

        private List<MugType> CollectGenericParameters()
        {
            var oldindex = _currentIndex;

            if (MatchAdvance(TokenKind.BooleanLess, true))
            {
                if (MatchType(out var type))
                {
                    var generics = new List<MugType>() { type };

                    while (MatchAdvance(TokenKind.Comma))
                        generics.Add(ExpectType());

                    if (MatchAdvance(TokenKind.BooleanGreater))
                        return generics;
                }
            }

            _currentIndex = oldindex;
            return new List<MugType>();
        }

        private bool MatchCallStatement(out INode e, INode name)
        {
            e = null;
            var token = Current;

            if (!MatchAdvance(TokenKind.OpenPar, true) && !Match(TokenKind.BooleanLess, true))
                return false;

            var parameters = new NodeBuilder();

            /*if (name is MemberNode instanceAccesses)
            {
                // parameters.Add(instanceAccesses.Base);

                name = instanceAccesses*//*.Member*//*;
            }*/

            var generics = CollectGenericParameters();

            if (token.Kind == TokenKind.BooleanLess && !MatchAdvance(TokenKind.OpenPar, true))
            {
                if (generics.Count != 0)
                    ParseError("Expected call after generic parameter specification");

                return false;
            }

            CollectParameters(ref parameters);
            
            e = new CallStatement() { Generics = generics, Name = name, Parameters = parameters, Position = name is null ? name.Position : name.Position };

            while (MatchAdvance(TokenKind.Dot))
            {
                name = Expect("Expected member after '.'", TokenKind.Identifier);

                generics = CollectGenericParameters();

                if (MatchAdvance(TokenKind.OpenPar, true))
                {
                    parameters = new NodeBuilder();

                    CollectParameters(ref parameters);

                    // parameters.Insert(0, e);

                    e = new CallStatement() { Generics = generics, Position = name.Position, Name = new MemberNode() { Base = e, Member = (Token)name, Position = new(e.Position.Lexer, e.Position.Position.Start..name.Position.Position.End) }, Parameters = parameters };
                }
                else
                {
                    if (generics.Count != 0)
                        ParseError("Expected call after generic parameter specification");

                    e = new MemberNode() { Base = e, Member = (Token)name, Position = new(e.Position.Lexer, e.Position.Position.Start..name.Position.Position.End) };
                }
            }

            return true;
        }

        private bool MatchPrefixOperator(out Token prefix)
        {
            return
                MatchAdvance(TokenKind.Minus, out prefix, true)             ||
                MatchAdvance(TokenKind.Plus, out prefix, true)              ||
                MatchAdvance(TokenKind.Negation, out prefix, true)          ||
                MatchAdvance(TokenKind.BooleanAND, out prefix, true)        ||
                MatchAdvance(TokenKind.Star, out prefix, true)              ||
                MatchAdvance(TokenKind.OperatorIncrement, out prefix, true) ||
                MatchAdvance(TokenKind.OperatorDecrement, out prefix, true);
        }

        internal static bool HasElseBody(ConditionalStatement condition)
        {
            while (condition is not null && condition.Expression is not null)
                condition = condition.ElseNode;

            return condition is not null && condition.Expression is null;
        }

        private bool MatchTerm(out INode e, bool allowCallStatement = true)
        {
            /*if (Current.Kind == TokenKind.EOL)
                Console.WriteLine("eol");*/

            if (MatchPrefixOperator(out var prefixOP))
            {
                if (!MatchTerm(out e, allowCallStatement))
                {
                    Report("Unexpected prefix operator");
                    return false;
                }

                e = new PrefixOperator() { Expression = e, Position = prefixOP.Position, Prefix = prefixOP.Kind };

                return true;
            }

            // arr[]
            // base.member
            // base.member()
            // base()
            // base.method().other_method()

            if (!MatchMember(out e))
            {
                if (MatchAdvance(TokenKind.KeyNew, out var token))
                    e = CollectNodeNew(token.Position);
                else if (ConditionDefinition(out e))
                    return true;
            }

            CollectPossibleArrayAccessNode(ref e);

            if (allowCallStatement && MatchCallStatement(out var call, e))
                e = call;

            CollectPossibleArrayAccessNode(ref e);

            if (MatchAdvance(TokenKind.OperatorIncrement, true) || MatchAdvance(TokenKind.OperatorDecrement, true))
                e = new PostfixOperator() { Expression = e, Position = Back.Position, Postfix = Back.Kind };

            return true;
        }

        private INode ExpectFactor()
        {
            if (!MatchFactor(out INode e) &&
                !MatchInParExpression(out e))
                ParseError("Expected expression factor here");

            return e;
        }

        private bool MatchFactorOps()
        {
            return
                MatchAdvance(TokenKind.Star, true) ||
                MatchAdvance(TokenKind.Slash, true) ||
                MatchAdvance(TokenKind.RangeDots, true);
        }

        private FieldAssignmentNode ExpectFieldAssign()
        {
            if (!MatchAdvance(TokenKind.Identifier))
            {
                Report("Expected field assignment");
                return null;
            }

            var name = Back;
            Expect($"When assigning a field, required ':', not '{Current.Value}'", TokenKind.Colon);

            var expression = ExpectExpression(true, true, false, TokenKind.Comma, TokenKind.CloseBrace);
            _currentIndex--;

            return new FieldAssignmentNode() { Name = name.Value.ToString(), Body = expression, Position = name.Position };
        }

        private INode ExpectTerm(bool allowCallStatement = true)
        {
            if (!MatchTerm(out var e, allowCallStatement))
                Report("Expected term");

            return e;
        }

        private bool MatchFactor(out INode e)
        {
            if (!MatchTerm(out e))
                return false;
            
            if (MatchFactorOps())
            {
                var op = Back;
                var right = ExpectTerm();
                do
                {
                    e = new ExpressionNode() { Left = e, Right = right, Operator = ToOperatorKind(op.Kind), Position = op.Position };
                    if (MatchFactorOps())
                        op = Back;
                    else
                        break;
                } while (MatchTerm(out right));
            }

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
            if (MatchAdvance(TokenKind.OpenBracket))
            {
                var type = ExpectType();
                INode size = null;

                if (MatchAdvance(TokenKind.Comma))
                {
                    size = ExpectExpression(end: TokenKind.CloseBracket);
                    _currentIndex--;
                }
                
                Expect("Expected ']' and the array body", TokenKind.CloseBracket);

                var array = new ArrayAllocationNode() { SizeIsImplicit = size == null, Size = size, Type = type };
                Expect("Expected the array body, empty ('{}') if has to be instanced with type default values", TokenKind.OpenBrace);

                if (!Match(TokenKind.CloseBrace))
                {
                    do
                    {
                        array.AddArrayElement(ExpectExpression(true, true, false, TokenKind.Comma, TokenKind.CloseBrace));
                        _currentIndex--;
                    }
                    while (MatchAdvance(TokenKind.Comma));
                }

                Expect("", TokenKind.CloseBrace);

                return array;
            }

            var name = ExpectType();
            var allocation = new TypeAllocationNode() { Name = name, Position = newposition };

            Expect("Type allocation requires '{}'", TokenKind.OpenBrace);

            if (Match(TokenKind.Identifier))
                do
                    allocation.Body.Add(ExpectFieldAssign());
                while (MatchAdvance(TokenKind.Comma));

            Expect("", TokenKind.CloseBrace);

            return allocation;
        }

        private bool MatchAndOrOperator()
        {
            return MatchAdvance(TokenKind.BooleanOR, true) || MatchAdvance(TokenKind.BooleanAND, true);
        }

        private bool MatchPlusMinus()
        {
            return
                MatchAdvance(TokenKind.Plus, true) ||
                MatchAdvance(TokenKind.Minus, true);
        }

        private INode ExpectExpression(bool allowBoolOP = true, bool allowLogicOP = true, bool allowNullExpression = false, params TokenKind[] end)
        {
            // skipping new line in expressions
            if (Current.Kind == TokenKind.EOL)
                _currentIndex++;

            if (MatchFactor(out var e))
            {
                if (MatchPlusMinus())
                {
                    var op = Back;
                    var right = ExpectFactor();

                    do
                    {
                        e = new ExpressionNode() { Operator = ToOperatorKind(op.Kind), Left = e, Right = right, Position = op.Position };
                        if (MatchPlusMinus())
                            op = Back;
                        else
                            break;
                    } while (MatchFactor(out right));
                }
            }

            if (e is null && !allowNullExpression)
            {
                Report($"Expected expression, found '{Current.Value}'");
                _currentIndex++; // skipping bad token
            }

            if (MatchAdvance(TokenKind.KeyAs, out var asToken))
                e = new CastExpressionNode() { Expression = e, Type = ExpectType(), Position = asToken.Position };

            if (MatchAdvance(TokenKind.KeyCatch))
            {
                var match = MatchAdvance(TokenKind.Identifier, out var error);

                e = new CatchExpressionNode() { Expression = e, OutError = match ? new Token?(error) : null, Position = Back.Position, Body = ExpectBlock() };
            }
            
            while (allowBoolOP && MatchBooleanOperator(out var boolOP))
                e = new BooleanExpressionNode() { Operator = ToOperatorKind(boolOP.Kind), Position = boolOP.Position, Left = e, Right = ExpectExpression(false, false, end: end) };

            while (allowLogicOP && MatchAndOrOperator())
                e = new BooleanExpressionNode() { Operator = ToOperatorKind(Back.Kind), Position = Back.Position, Left = e, Right = ExpectExpression(allowLogicOP: false, end: end) };

            if (allowBoolOP && allowLogicOP) // if is first call
            {
                if (MatchAssigmentOperators(out var @operator))
                    e = new AssignmentStatement() { Name = e, Operator = @operator.Kind, Position = @operator.Position, Body = ExpectExpression(end: end) };

                ExpectMultiple($"Invalid token in the current context, maybe missing one of '{TokenKindsToString(end)}'", end);
            }

            return e;
        }

        private MugType ExpectVariableType()
        {
            return MatchAdvance(TokenKind.Colon) ? ExpectType() : MugType.Automatic(Back.Position);
        }

        private bool VariableDefinition(out INode statement)
        {
            statement = null;

            if (!MatchAdvance(TokenKind.KeyVar))
                return false;

            var name = Expect("Expected the variable name", TokenKind.Identifier);
            var type = ExpectVariableType();
            INode body = MatchAdvance(TokenKind.Equal) ? ExpectExpression(true) : null;

            statement = new VariableStatement() { Body = body, Name = name.Value.ToString(), Position = name.Position, Type = type };

            return true;
        }

        private bool ConstantDefinition(out INode statement)
        {
            statement = null;

            if (!MatchAdvance(TokenKind.KeyConst))
                return false;

            var name = Expect("Expected the constant name", TokenKind.Identifier);
            var type = ExpectVariableType();

            Expect("A constant cannot be declared without a body", TokenKind.Equal);

            var body = ExpectExpression(true);
            statement = new ConstantStatement() { Body = body, Name = name.Value.ToString(), Position = name.Position, Type = type };

            return true;
        }

        private bool ReturnDeclaration(out INode statement)
        {
            statement = null;

            if (!MatchAdvance(TokenKind.KeyReturn))
                return false;

            var pos = Back.Position;
            
            statement = new ReturnStatement() { Position = pos, Body = ExpectExpression(allowNullExpression: true) };

            return true;
        }

        private bool MatchAssigmentOperators(out Token @operator)
        {
            return
                MatchAdvance(TokenKind.Equal, out @operator) ||
                MatchAdvance(TokenKind.AddAssignment, out @operator) ||
                MatchAdvance(TokenKind.SubAssignment, out @operator) ||
                MatchAdvance(TokenKind.MulAssignment, out @operator) ||
                MatchAdvance(TokenKind.DivAssignment, out @operator);
        }

        private bool ConditionDefinition(out INode statement, bool isFirstCondition = true)
        {
            statement = null;

            if (!MatchAdvance(TokenKind.KeyIf, out Token key) &&
                !MatchAdvance(TokenKind.KeyElif, out key) &&
                !MatchAdvance(TokenKind.KeyElse, out key) &&
                !MatchAdvance(TokenKind.KeyWhile, out key))
                return false;

            if (isFirstCondition && key.Kind != TokenKind.KeyIf && key.Kind != TokenKind.KeyWhile)
            {
                _currentIndex--;
                Report("The 'elif' and 'else' conditions shall be referenced to an 'if' block");
                return false;
            }

            INode expression = null;
            if (key.Kind != TokenKind.KeyElse)
            {
                expression = ExpectExpression(end: TokenKind.OpenBrace);
                _currentIndex--;
            }

            var body = ExpectBlock();

            INode elif = null;
            if (key.Kind != TokenKind.KeyWhile && key.Kind != TokenKind.KeyElse && (Match(TokenKind.KeyElif) || Match(TokenKind.KeyElse)))
                ConditionDefinition(out elif, false);

            statement = new ConditionalStatement() { Position = key.Position, Expression = expression, Kind = key.Kind, Body = body, ElseNode = (ConditionalStatement)elif };

            return true;
        }

        private VariableStatement CollectForLeftExpression()
        {
            if (MatchAdvance(TokenKind.Comma))
                return null;

            var name = Expect("Expected left statement or comma", TokenKind.Identifier);
            var result = new VariableStatement() { Name = name.Value, Position = name.Position };

            if (MatchAdvance(TokenKind.Colon))
            {
                result.Type = ExpectType();
                Expect("", TokenKind.Comma);
            }
            else
            {
                result.Type = MugType.Automatic(name.Position);
                Expect("Type notation or body needed", TokenKind.Equal);
                result.Body = ExpectExpression(end: TokenKind.Comma);
            }

            return result;
        }

        private T CollectOrNull<T>(string error, TokenKind end) where T: class, INode
        {
            if (MatchAdvance(end))
                return null;

            var pos = Current.Position;
            var expr = ExpectExpression(end: end);
            if (expr is not T)
                ParseError(pos, error);

            return (T)expr;
        }

        private bool ForLoopDefinition(out INode statement)
        {
            statement = null;

            if (!MatchAdvance(TokenKind.KeyFor, out Token key))
                return false;

            var leftexpr = CollectForLeftExpression();
            var conditionexpr = CollectOrNull<INode>("Expected condition expression or comma", TokenKind.Comma);
            var rightexpr = CollectOrNull<IStatement>("Expected right statement or nothing", TokenKind.OpenBrace);

            _currentIndex--; // returning on '{' token

            statement = new ForLoopStatement()
            {
                Body = ExpectBlock(),
                LeftExpression = leftexpr,
                ConditionExpression = conditionexpr,
                RightExpression = rightexpr,
                Position = key.Position
            };

            return true;
        }

        private bool LoopManagerDefintion(out INode statement)
        {
            statement = null;

            if (!MatchAdvance(TokenKind.KeyContinue) &&
                !MatchAdvance(TokenKind.KeyBreak))
                return false;

            statement = new LoopManagementStatement() { Management = Back, Position = Back.Position };

            return true;
        }

        private INode ExpectStatement()
        {
            if (!VariableDefinition(out var statement) && // var x = value;
                !ReturnDeclaration(out statement) && // return value;
                !ConstantDefinition(out statement) && // const x = value;
                !ConstantDefinition(out statement) && // const x = value;
                !MatchWhenStatement(out statement, false) && // when comptimecondition {}
                !ConditionDefinition(out statement) &&
                !ForLoopDefinition(out statement) && // for x: type to, in value {}
                !LoopManagerDefintion(out statement)) // continue, break
                statement = ExpectExpression(true);

            return statement;
        }

        private BlockNode ExpectBlock()
        {
            Expect("", TokenKind.OpenBrace);

            var block = new BlockNode();

            while (!Match(TokenKind.CloseBrace))
                block.Add(ExpectStatement());

            Expect("", TokenKind.CloseBrace);

            return block;
        }

        private ParameterListNode ExpectParameterListDeclaration()
        {
            Expect("", TokenKind.OpenPar);

            var parameters = new ParameterListNode();
            var count = 0;

            while (!MatchAdvance(TokenKind.ClosePar))
            {
                parameters.Parameters.Add(ExpectParameter(count == 0));
                count++;
            }

            return parameters;
        }

        private TokenKind GetModifier()
        {
            if (_modifier == TokenKind.Bad)
                return TokenKind.KeyPriv;

            var old = _modifier;

            _modifier = TokenKind.Bad;

            return old; // pub only currently
        }

        private Pragmas GetPramas()
        {
            var result = _pragmas;
            _pragmas = null;
            return result is not null ? result : new();
        }

        private ParameterNode? CollectBaseDefinition()
        {
            if (!MatchAdvance(TokenKind.OpenPar))
                return null;

            var name = Expect("Expected the base instance name", TokenKind.Identifier);
            Expect("", TokenKind.Colon);
            var type = ExpectType();
            Expect("", TokenKind.ClosePar);

            return new ParameterNode(type, name.Value, new(), name.Position);
        }

        private bool FunctionDefinition(out INode node)
        {
            node = null;

            if (!MatchAdvance(TokenKind.KeyFunc)) // <func>
                return false;

            var modifier = GetModifier();
            var pragmas = GetPramas();
            var generics = new List<Token>();

            var @base = CollectBaseDefinition();

            var name = Expect("In function definition must specify the name", TokenKind.Identifier); // func <name>

            CollectGenericParameterDefinitions(generics);

            var parameters = ExpectParameterListDeclaration(); // func name<(..)>

            MugType type;

            if (MatchAdvance(TokenKind.Colon))
                type = ExpectType(true);
            else
                type = new MugType(name.Position, TypeKind.Void);

            if (Match(TokenKind.OpenBrace)) // function definition
            {
                var body = ExpectBlock();

                var f = new FunctionNode() { Base = @base, Modifier = modifier, Pragmas = pragmas, Body = body, Name = name.Value.ToString(), ParameterList = parameters, ReturnType = type, Position = name.Position };

                f.Generics = generics;

                node = f;
            }
            else // prototype
            {
                if (@base is not null)
                    ParseError(@base.Value.Position, "The function base cannot be defined in function prototypes");

                var f = new FunctionPrototypeNode() { Modifier = modifier, Pragmas = pragmas, Name = name.Value.ToString(), ParameterList = parameters, Type = type, Position = name.Position };
                f.Generics = generics;

                node = f;
            }

            return true;
        }

        private bool MatchImportDirective(out INode directive)
        {
            directive = null;

            if (!MatchAdvance(TokenKind.KeyImport, out var token)) // <import>
                return false;

            INode body;

            ImportMode mode = ImportMode.FromPackages;

            if (MatchAdvance(TokenKind.ConstantString)) // import <"path">
            {
                body = Back;
                mode = ImportMode.FromLocal;
            }
            else // import <path>
                body = Expect("Expected import's path", TokenKind.Identifier);

            directive = new ImportDirective() { Mode = mode, Member = body, Position = token.Position };
            return true;
        }

        private bool MatchUseDirective(out INode directive)
        {
            directive = null;

            if (!MatchAdvance(TokenKind.KeyUse, out var token)) // <use>
                return false;

            var body = Expect("", TokenKind.Identifier); // use <path>

            Expect("Allowed only alias declaration with use directive", TokenKind.KeyAs); // use path <as>

            var alias = Expect("Expected the use path alias, after 'as' in a use directive", TokenKind.Identifier); // use path as <alias>

            directive = new UseDirective() { Body = body, Alias = alias, Position = token.Position };
            return true;
        }

        private bool MatchDeclareDirective(out INode directive)
        {
            directive = null;

            if (!MatchAdvance(TokenKind.KeyDeclare, out var token))
                return false;

            directive = new DeclareDirective() { Position = token.Position, Symbol = Expect("Expected symbol", TokenKind.Identifier) };
            return true;
        }

        private CompTimeExpression ExpectCompTimeExpression()
        {
            CompTimeExpression comptimeExpr = new();
            var boolOP = new Token(TokenKind.Bad, null, new());

            do
            {
                if (boolOP.Kind != TokenKind.Bad)
                    comptimeExpr.Expression.Add(boolOP);

                comptimeExpr.Expression.Add(Expect("Expected symbol", TokenKind.Identifier));

            } while (MatchAdvance(TokenKind.BooleanAND, out boolOP) || MatchAdvance(TokenKind.BooleanOR, out boolOP));

            return comptimeExpr;
        }

        private NodeBuilder ExpectWhenBlockGlobalScope()
        {
            Expect("Expected when body", TokenKind.OpenBrace);

            var members = ExpectNamespaceMembers(TokenKind.CloseBrace);

            Expect("", TokenKind.CloseBrace);

            return members;
        }

        private bool MatchWhenStatement(out INode directive, bool isGlobalScope)
        {
            directive = null;

            if (!MatchAdvance(TokenKind.KeyWhen, out var token))
                return false;

            var expression = ExpectCompTimeExpression();

            directive = new CompTimeWhenStatement()
            {
                Position = token.Position,
                Expression = expression,
                Body = isGlobalScope ? (object)ExpectWhenBlockGlobalScope() : (object)ExpectBlock()
            };
            return true;
        }

        private bool DirectiveDefinition(out INode directive)
        {
            return
                MatchImportDirective(out directive)  ||
                MatchUseDirective(out directive)     ||
                MatchDeclareDirective(out directive) ||
                MatchWhenStatement(out directive, true);
        }

        private FieldNode ExpectFieldDefinition()
        {
            var name = Expect("Expected field's name", TokenKind.Identifier); // <field>

            if (!MatchAdvance(TokenKind.Colon))
            {
                Report(UnexpectedToken);
                return null;
            }

            var type = ExpectType(); // field: <error>

            return new FieldNode() { Name = name.Value.ToString(), Type = type, Position = name.Position };
        }

        private void CollectGenericParameterDefinitions(List<Token> generics)
        {
            if (MatchAdvance(TokenKind.BooleanLess))
            {
                do
                    generics.Add(Expect("Expected generic name", TokenKind.Identifier));
                while (MatchAdvance(TokenKind.Comma));

                Expect("", TokenKind.BooleanGreater);
            }
        }

        /// <summary>
        /// search for a struct definition
        /// </summary>
        private bool TypeDefinition(out INode node)
        {
            node = null;

            // returns if does not match a type keyword
            if (!MatchAdvance(TokenKind.KeyType))
                return false;

            // required an identifier
            var modifier = GetModifier();
            var pragmas = GetPramas();
            var name = Expect("Expected the type name after 'type' keyword", TokenKind.Identifier);
            var statement = new TypeStatement() { Modifier = modifier, Pragmas = pragmas, Name = name.Value.ToString(), Position = name.Position };

            // struct generics
            CollectGenericParameterDefinitions(statement.Generics);

            // struct body
            Expect(UnexpectedToken, TokenKind.OpenBrace);

            while (Match(TokenKind.Identifier))
                statement.Body.Add(ExpectFieldDefinition());

            if (statement.Body.Count == 0)
                Report(name.Position, "Structure must contain at least one member");

            Expect(UnexpectedToken, TokenKind.CloseBrace); // expected close body

            node = statement;

            return true;
        }

        private EnumMemberNode ExpectMemberDefinition(bool basetypeisint, int lastvalue)
        {
            var name = Expect("Expected enum's member name", TokenKind.Identifier);
            var usedimplicitconstant = false;

            if (!MatchAdvance(TokenKind.Colon))
            {
                if (basetypeisint)
                    usedimplicitconstant = true;
                else
                    Report(name.Position, "Enum member must have an explicit constant value when base type is not int");
            }
            else if (!MatchConstantAdvance())
            {
                Report("Invalid member's explicit constant value");
                return null;
            }
            
            return new EnumMemberNode()
            {
                Name = name.Value,
                Value = usedimplicitconstant ? new Token(TokenKind.ConstantDigit, (lastvalue + 1).ToString(), name.Position) : Back,
                Position = name.Position };
        }

        private MugType ExpectPrimitiveType()
        {
            if (!MatchPrimitiveType(out var type))
            {
                Report("Expected primitive type");
                return MugType.Automatic(type.Position);
            }

            return MugType.FromToken(type);
        }

        private void Report(string error)
        {
            Lexer.DiagnosticBag.Report(Current.Position, error);
        }

        private void Report(ModulePosition position, string error)
        {
            Lexer.DiagnosticBag.Report(position, error);
        }

        private bool EnumDefinition(out INode node)
        {
            node = null;

            // returns if does not match a type keyword
            if (!MatchAdvance(TokenKind.KeyEnum))
                return false;

            var errorsNum = Lexer.DiagnosticBag.Count;

            // required an identifier
            var modifier = GetModifier();
            var pragmas = GetPramas();
            var name = Expect("Expected the type name after 'enum' keyword", TokenKind.Identifier);
            var statement = new EnumStatement() { Modifier = modifier, Pragmas = pragmas, Name = name.Value.ToString(), Position = name.Position };

            // base type
            Expect("An enum must have a primitive base type", TokenKind.Colon);

            if (errorsNum != Lexer.DiagnosticBag.Count)
                return false;

            statement.BaseType = ExpectPrimitiveType();

            // enum body
            Expect(UnexpectedToken, TokenKind.OpenBrace);

            while (Match(TokenKind.Identifier))
            {
                var value = -1;
                if (statement.Body.Count > 0)
                    int.TryParse(statement.Body.Last().Value.Value, out value);

                var member = ExpectMemberDefinition(statement.BaseType.IsInt(), value);
                if (member is null)
                    return false;

                statement.Body.Add(member);
            }

            Expect(UnexpectedToken, TokenKind.CloseBrace); // expected close body

            node = statement;

            return true;
        }

        private void CollectPragmas()
        {
            if (!MatchAdvance(TokenKind.OpenBracket))
                return;

            _pragmas = new();

            do
            {
                var name = Expect("Expected pragma's name", TokenKind.Identifier).Value;
                Expect("", TokenKind.Colon);
                var value = ExpectConstant("Non-constant expressions not allowed in pragmas");

                _pragmas.SetPragma(name, value, ParseError, ref _currentIndex);
            } while (MatchAdvance(TokenKind.Comma));

            Expect("Expected pragmas close", TokenKind.CloseBracket);
        }

        private void CollectModifier()
        {
            if (MatchAdvance(TokenKind.KeyPub) || MatchAdvance(TokenKind.KeyPriv))
                _modifier = Back.Kind;
        }

        /// <summary>
        /// expects at least one member
        /// </summary>
        private NodeBuilder ExpectNamespaceMembers(TokenKind end = TokenKind.EOF)
        {
            NodeBuilder nodes = new();

            // while the current token is not end
            while (!Match(end))
            {
                // collecting pragmas for first two statements (function and type)
                CollectPragmas();
                CollectModifier();

                // searches for a global statement
                // func id() {}
                if (!FunctionDefinition(out INode statement))
                    // (c struct) type MyStruct {}
                    if (!TypeDefinition(out statement))
                    {
                        if (!EnumDefinition(out statement))
                        {
                            if (_pragmas is not null)
                                Report("Invalid pragmas for this member");

                            if (_modifier != TokenKind.Bad)
                                Report("Invalid modifier for this member");

                            // var id = constant;
                            if (!VariableDefinition(out statement))
                                // import "", import path, use x as y
                                if (!DirectiveDefinition(out statement))
                                    _currentIndex++; // skipping the bad token
                        }
                    }

                // adds the statement to the members
                nodes.Add(statement);
            }

            return nodes;
        }

        /// <summary>
        /// generates the ast from a tokens stream
        /// </summary>
        public NamespaceNode Parse()
        {
            // to avoid bugs
            if (Match(TokenKind.EOF))
                return Module;

            Module.Name = new Token(TokenKind.Identifier, Lexer.ModuleName, new(Lexer, 0..(Lexer.Source.Length - 1)));

            // search for members
            Module.Members = ExpectNamespaceMembers();
            
            // breaking the compiler workflow if diagnostic is bad
            Lexer.CheckDiagnostic();

            return Module;
        }
    }
}