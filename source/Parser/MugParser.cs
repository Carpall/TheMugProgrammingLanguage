using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser.AST;
using Mug.Models.Parser.AST.Directives;
using Mug.Models.Parser.AST.Statements;
using Mug.TypeSystem;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mug.Models.Parser
{
    public class MugParser : MugComponent
    {
        public NamespaceNode Module { get; } = new();
        private int CurrentIndex { get; set; }

        private Pragmas _pragmas = null;
        private TokenKind _modifier = TokenKind.Bad;

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

        public MugParser(CompilationTower tower) : base(tower)
        {
        }

        private Token Current
        {
            get
            {
                if (CurrentIndex >= Tower.TokenCollection.Count)
                    ParseErrorEOF("");

                return Tower.TokenCollection[CurrentIndex];
            }
        }

        private Token Back
        {
            get
            {
                if (CurrentIndex - 1 >= 0)
                    return Tower.TokenCollection[CurrentIndex - 1];

                return new Token();
            }
        }

        private string UnexpectedToken => $"Unexpected token '{Current.Value}'";

        private static string TokenKindsToString(TokenKind[] kinds)
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
                        CurrentIndex++;
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

            CurrentIndex++;
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
                CurrentIndex++;

            return match;
        }

        private MugType ExpectType(bool allowEnumError = false)
        {
            if (MatchAdvance(TokenKind.BooleanAND, out var token))
                return UnsolvedType.Create(
                    Tower,
                    token.Position,
                    TypeKind.Reference,
                    ExpectType(false));

            if (MatchAdvance(TokenKind.OpenBracket, out token))
            {
                var type = ExpectType();
                Expect("An array type definition must end by ']'", TokenKind.CloseBracket);
                return UnsolvedType.Create(
                    Tower,
                    new(token.Position.Lexer, token.Position.Position.Start..Back.Position.Position.End),
                    TypeKind.Array,
                    type);
            }
            else if (MatchAdvance(TokenKind.Star, out token))
            {
                var type = ExpectType();
                return UnsolvedType.Create(
                    Tower,
                    new(token.Position.Lexer, token.Position.Position.Start..type.UnsolvedType.Value.Position.Position.End),
                    TypeKind.Pointer,
                    type);
            }

            var find = ExpectBaseType();

            // struct generics
            if (MatchAdvance(TokenKind.BooleanLess))
            {
                if (find.UnsolvedType.Value.Kind != TypeKind.DefinedType)
                {
                    CurrentIndex -= 2;
                    ParseError($"Generic parameters cannot be passed to type '{find}'");
                }

                var genericTypes = new List<MugType>();

                do
                    genericTypes.Add(ExpectType());
                while (MatchAdvance(TokenKind.Comma));

                Expect("", TokenKind.BooleanGreater);

                find = UnsolvedType.Create(
                    Tower,
                    new(find.UnsolvedType.Value.Position.Lexer, find.UnsolvedType.Value.Position.Position.Start..Back.Position.Position.End),
                    TypeKind.GenericDefinedType,
                    (find, genericTypes));
            }

            if (allowEnumError && MatchAdvance(TokenKind.Negation))
                find = UnsolvedType.Create(Tower, Back.Position, TypeKind.EnumError, (find, ExpectType()));

            return find;
        }

        private bool MatchType(out MugType type)
        {
            type = null;
            MugType t = null;

            if (!Match(TokenKind.OpenBracket) && !Match(TokenKind.Star) && !MatchBaseType(out t))
                return false;

            if (t is not null)
                CurrentIndex--;

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
            CurrentIndex--;

            return new ParameterNode(type, name.Value, defaultvalue, name.Position);
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

            type = UnsolvedType.FromToken(Tower, token);

            return true;
        }

        private bool MatchPrimitiveType(out Token type, bool allowErr = false)
        {
            type = Current;

            return
                MatchSpecificIdentifier("str") ||
                MatchSpecificIdentifier("chr") ||
                MatchSpecificIdentifier("u8") ||
                MatchSpecificIdentifier("i32") ||
                MatchSpecificIdentifier("i64") ||
                MatchSpecificIdentifier("f32") ||
                MatchSpecificIdentifier("f64") ||
                MatchSpecificIdentifier("f128") ||
                MatchSpecificIdentifier("void") ||
                MatchSpecificIdentifier("bool") ||
                MatchSpecificIdentifier("unknown") ||
                (allowErr && MatchSpecificIdentifier("err"));
        }

        private bool MatchValue()
        {
            return
                MatchAdvance(TokenKind.Identifier) ||
                MatchConstantAdvance();
        }

        private bool MatchConstantAdvance()
        {
            return
                MatchAdvance(TokenKind.ConstantChar) ||
                MatchAdvance(TokenKind.ConstantDigit) ||
                MatchAdvance(TokenKind.ConstantFloatDigit) ||
                MatchAdvance(TokenKind.ConstantString) ||
                MatchAdvance(TokenKind.ConstantBoolean);
        }

        private bool MatchInParExpression(out INode e)
        {
            e = null;

            if (!MatchAdvance(TokenKind.OpenPar))
                return false;

            e = ExpectExpression(end: TokenKind.ClosePar);
            
            return true;
        }

        private void CollectPossibleArrayAccessNode(ref INode e)
        {
            if (e is null)
                return;

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
                parameters.Nodes.Add(ExpectExpression(end: new[] { TokenKind.Comma, TokenKind.ClosePar}));

                lastiscomma = Back.Kind == TokenKind.Comma;
                if (Back.Kind == TokenKind.ClosePar)
                {
                    lastiscomma = false;
                    CurrentIndex--;
                }
            }

            if (lastiscomma)
                Report(Back.Position, "Expected parameter's expression");
        }

        private List<MugType> CollectGenericParameters(ref bool builtin)
        {
            var oldindex = CurrentIndex;

            if (MatchAdvance(TokenKind.BooleanLess, true))
            {
                if (builtin)
                    ParseError(UnexpectedToken);

                if (MatchType(out var type))
                {
                    var generics = new List<MugType>() { type };

                    while (MatchAdvance(TokenKind.Comma))
                        generics.Add(ExpectType());

                    if (MatchAdvance(TokenKind.BooleanGreater))
                    {
                        builtin = CollectBuiltInSymbol();
                        return generics;
                    }
                }
            }

            CurrentIndex = oldindex;
            return new List<MugType>();
        }

        private bool CollectBuiltInSymbol()
        {
            return MatchAdvance(TokenKind.Negation);
        }

        private bool MatchCallStatement(out INode e, INode name)
        {
            e = null;

            var builtin = CollectBuiltInSymbol();

            if (!Match(TokenKind.OpenPar, true) && !Match(TokenKind.BooleanLess, true))
                return false;

            var generics = CollectGenericParameters(ref builtin);

            if (!MatchAdvance(TokenKind.OpenPar))
            {
                if (generics.Count > 0)
                    Report("Invalid generic parameters here");

                return false;
            }

            var parameters = new NodeBuilder();
            CollectParameters(ref parameters);

            e = new CallStatement()
            {
                Generics = generics,
                IsBuiltIn = builtin,
                Name = name,
                Parameters = parameters,
                Position = name is null ? name.Position : name.Position
            };

            CollectCatchExpression(ref e);

            return true;
        }

        private void CollectCatchExpression(ref INode e)
        {
            if (MatchAdvance(TokenKind.KeyCatch))
            {
                var match = MatchAdvance(TokenKind.Identifier, out var error);

                e = new CatchExpressionNode()
                {
                    Expression = e,
                    OutError = match ? new Token?(error) : null,
                    Position = Back.Position,
                    Body = ExpectBlock()
                };
            }
        }

        private void CollectDotExpression(ref INode e)
        {
            var name = Expect("Expected member after '.'", TokenKind.Identifier);

            var builtin = CollectBuiltInSymbol();

            var generics = CollectGenericParameters(ref builtin);

            if (MatchAdvance(TokenKind.OpenPar, true))
            {
                var parameters = new NodeBuilder();

                CollectParameters(ref parameters);

                e = new CallStatement() { Generics = generics, IsBuiltIn = builtin, Position = name.Position, Name = new MemberNode() { Base = e, Member = name, Position = new(e.Position.Lexer, e.Position.Position.Start..name.Position.Position.End) }, Parameters = parameters };
            }
            else
            {
                if (generics.Count != 0)
                    ParseError("Expected call after generic parameter specification");

                e = new MemberNode() { Base = e, Member = name, Position = new(e.Position.Lexer, e.Position.Position.Start..name.Position.Position.End) };
            }
        }

        private bool MatchPrefixOperator(out Token prefix)
        {
            return
                MatchAdvance(TokenKind.Minus, out prefix)             ||
                MatchAdvance(TokenKind.Plus, out prefix)              ||
                MatchAdvance(TokenKind.Negation, out prefix)          ||
                MatchAdvance(TokenKind.BooleanAND, out prefix)        ||
                MatchAdvance(TokenKind.Star, out prefix)              ||
                MatchAdvance(TokenKind.OperatorIncrement, out prefix) ||
                MatchAdvance(TokenKind.OperatorDecrement, out prefix);
        }

        internal static bool HasElseBody(ConditionalStatement condition)
        {
            while (condition is not null && condition.Expression is not null)
                condition = condition.ElseNode;

            return condition is not null && condition.Expression is null;
        }

        private bool MatchTerm(out INode e, bool allowNullExpression)
        {
            if (MatchAdvance(TokenKind.KeyTry))
            {
                if (!MatchTerm(out e, false) || e is not CallStatement)
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

            if (MatchPrefixOperator(out var prefixOP))
            {
                if (!MatchTerm(out e, allowNullExpression))
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

            if (!allowNullExpression && e is null)
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

        private void CollectPostfixOperator(ref INode e)
        {
            if (MatchAdvance(TokenKind.OperatorIncrement, true) || MatchAdvance(TokenKind.OperatorDecrement, true))
                e = new PostfixOperator() { Expression = e, Position = Back.Position, Postfix = Back.Kind };
        }

        private void CollectPossibleDotExpression(ref INode e)
        {
            while (MatchAdvance(TokenKind.Dot))
                CollectDotExpression(ref e);
        }

        private INode ExpectFactor(bool allowNullExpression)
        {
            if (!MatchFactor(out INode e, allowNullExpression))
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
            CurrentIndex--;

            return new FieldAssignmentNode() { Name = name.Value.ToString(), Body = expression, Position = name.Position };
        }

        private INode ExpectTerm(bool allowNullExpression)
        {
            if (!MatchTerm(out var e, allowNullExpression))
                Report(UnexpectedToken);

            return e;
        }

        private bool MatchFactor(out INode e, bool allowNullExpression)
        {
            if (!MatchTerm(out e, allowNullExpression))
                return false;

            if (MatchFactorOps())
            {
                var op = Back;
                var right = ExpectTerm(allowNullExpression);
                do
                {
                    e = new BinaryExpressionNode() { Left = e, Right = right, Operator = op.Kind, Position = op.Position };
                    if (MatchFactorOps())
                        op = Back;
                    else
                        break;
                } while (MatchTerm(out right, allowNullExpression));
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
                MatchAdvance(TokenKind.KeyIs, out op) ||
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
                    CurrentIndex--;
                }
                
                Expect("Expected ']' and the array body", TokenKind.CloseBracket);

                var array = new ArrayAllocationNode() { SizeIsImplicit = size == null, Size = size, Type = type };
                Expect("Expected the array body, empty ('{}') if has to be instanced with type default values", TokenKind.OpenBrace);

                if (!Match(TokenKind.CloseBrace))
                {
                    do
                    {
                        array.Body.Add(ExpectExpression(true, true, false, TokenKind.Comma, TokenKind.CloseBrace));
                        CurrentIndex--;
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

        private INode ExpectExpression(
            bool allowBoolOP = true,
            bool allowLogicOP = true,
            bool allowNullExpression = false,
            params TokenKind[] end)
        {
            if (MatchFactor(out var e, allowNullExpression) && MatchPlusMinus())
            {
                var op = Back;
                var right = ExpectFactor(allowNullExpression);

                do
                {
                    e = new BinaryExpressionNode() { Operator = op.Kind, Left = e, Right = right, Position = op.Position };
                    if (MatchPlusMinus())
                        op = Back;
                    else
                        break;
                } while (MatchFactor(out right, allowNullExpression));
            }

            if (e is null && !allowNullExpression)
            {
                Report($"Expected expression, found '{Current.Value}'");
                CurrentIndex++; // skipping bad token
            }

            while (allowBoolOP && MatchBooleanOperator(out var boolOP))
            {
                var boolean = new BooleanBinaryExpressionNode()
                {
                    Operator = boolOP.Kind,
                    Left = e,
                    Position = boolOP.Position
                };

                if (boolOP.Kind != TokenKind.KeyIs)
                    boolean.Right = ExpectExpression(false, false, end: end);
                else
                {
                    boolean.IsInstructionType = ExpectType();
                    if (MatchAdvance(TokenKind.Identifier, out var alias, true))
                        boolean.IsInstructionAlias = alias;
                }

                e = boolean;
            }

            while (allowLogicOP && MatchAndOrOperator())
                e = new BooleanBinaryExpressionNode()
                {
                    Operator = Back.Kind,
                    Left = e,
                    Right = ExpectExpression(allowLogicOP: false, end: end),
                    Position = Back.Position
                };

            if (MatchAssigmentOperators(out var @operator))
                e = new AssignmentStatement()
                {
                    Name = e,
                    Operator = @operator.Kind,
                    Position = @operator.Position,
                    Body = ExpectExpression()
                };

            if (allowBoolOP && allowLogicOP) // if is first call
                ExpectMultiple($"Invalid token here, missing one of '{TokenKindsToString(end)}'?", end);

            return e;
        }

        private void CollectAsExpression(ref INode e)
        {
            if (MatchAdvance(TokenKind.KeyAs, out var token))
                e = new CastExpressionNode() { Expression = e, Type = ExpectType(), Position = token.Position };
        }

        private MugType ExpectVariableType()
        {
            return MatchAdvance(TokenKind.Colon) ? ExpectType() : UnsolvedType.Automatic(Tower, Back.Position);
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

            if (!MatchAdvance(TokenKind.Equal))
            {
                Report(name.Position, "A constant cannot be declared without a body");
                return true;
            }

            var body = ExpectExpression(true);
            statement = new ConstantStatement() { Body = body, Name = name.Value.ToString(), Position = name.Position, Type = type };

            return true;
        }

        private bool NextIsOnSameLine()
        {
            return CurrentIndex < Tower.TokenCollection.Count && !Tower.TokenCollection[CurrentIndex].IsOnNewLine;
        }

        private bool ReturnDeclaration(out INode statement)
        {
            statement = null;

            if (!MatchAdvance(TokenKind.KeyReturn))
                return false;

            var pos = Back.Position;

            statement = new ReturnStatement()
            {
                Position = pos,
                Body = !NextIsOnSameLine() ? null : ExpectExpression(allowNullExpression: true)
            };

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

        private bool CollectMatchExpression(out INode statement, ModulePosition position)
        {
            var matchexpr = new SwitchExpression() { Position = position, Expression = ExpectExpression(end: TokenKind.OpenBrace) };

            while (!MatchAdvance(TokenKind.CloseBrace))
            {
                var expression = (INode)null;
                var pos = Current.Position;

                if (!MatchAdvance(TokenKind.KeyElse))
                {
                    expression = ExpectExpression(end: TokenKind.OpenBrace);
                    pos = expression.Position;
                    CurrentIndex--;
                }

                matchexpr.Body.Add(new SwitchNode()
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
            statement = null;

            if (isFirstCondition && MatchAdvance(TokenKind.KeySwitch, out var matchtoken))
                return CollectMatchExpression(out statement, matchtoken.Position);

            if (!MatchAdvance(TokenKind.KeyIf, out Token key) &&
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

            INode expression = null;
            if (key.Kind != TokenKind.KeyElse)
            {
                expression = ExpectExpression(end: TokenKind.OpenBrace);
                CurrentIndex--;
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
                result.Type = UnsolvedType.Automatic(Tower, name.Position);
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

            CurrentIndex--; // returning on '{' token

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
                block.Statements.Add(ExpectStatement());

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
                type = UnsolvedType.Create(Tower, name.Position, TypeKind.Void);

            if (Match(TokenKind.OpenBrace)) // function definition
            {
                var body = ExpectBlock();

                var f = new FunctionStatement() { Base = @base, Modifier = modifier, Pragmas = pragmas, Body = body, Name = name.Value.ToString(), ParameterList = parameters, ReturnType = type, Position = name.Position };

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

        private bool DirectiveDefinition(out INode directive)
        {
            return
                MatchImportDirective(out directive) ||
                MatchUseDirective(out directive);
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

        private void ExpectVariantDefinition(out INode node, Token name, Pragmas pragmas, TokenKind modifier)
        {
            node = null;
            var variant = new VariantStatement() { Pragmas = pragmas, Modifier = modifier, Name = name.Value, Position = name.Position };

            Expect("", TokenKind.OpenPar);

            if (MatchAdvance(TokenKind.ClosePar))
            {
                report();
                return;
            }

            do
                variant.Body.Add(ExpectType());
            while (MatchAdvance(TokenKind.BooleanOR));

            Expect("", TokenKind.ClosePar);

            report();

            node = variant;

            void report()
            {
                if (variant.Body.Count < 2)
                    Report(name.Position, "Expected at least one type in variants");
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

            // variant definition
            if (MatchAdvance(TokenKind.Equal))
            {
                ExpectVariantDefinition(out node, name, pragmas, modifier);
                return true;
            }

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
                Value = usedimplicitconstant ? new Token(TokenKind.ConstantDigit, (lastvalue + 1).ToString(), name.Position, false) : Back,
                Position = name.Position
            };
        }

        private MugType ExpectPrimitiveType(bool allowErr)
        {
            if (!MatchPrimitiveType(out var type, allowErr))
            {
                Report("Expected primitive type");
                return UnsolvedType.Automatic(Tower, type.Position);
            }

            return UnsolvedType.FromToken(Tower, type, true);
        }

        private void Report(string error)
        {
            Tower.Report(Current.Position, error);
        }

        private void Report(ModulePosition position, string error)
        {
            Tower.Report(position, error);
        }

        private bool MatchSpecificIdentifier(string value, bool linesensitive = false)
        {
            if (MatchAdvance(TokenKind.Identifier, linesensitive))
            {
                var match = Back.Value == value;
                if (!match)
                    CurrentIndex--;

                return match;
            }
            else
                return false;// MatchAdvance(TokenKind.Identifier, linesensitive) && Back.Value == value;
        }

        private MugType ExpectEnumBaseType()
        {
            return ExpectPrimitiveType(true);
        }

        private bool EnumDefinition(out INode node)
        {
            node = null;

            // returns if does not match a type keyword
            if (!MatchAdvance(TokenKind.KeyEnum))
                return false;

            var errorsNum = Tower.Diagnostic.Count;

            // required an identifier
            var modifier = GetModifier();
            var pragmas = GetPramas();
            var name = Expect("Expected the type name after 'enum' keyword", TokenKind.Identifier);
            var statement = new EnumStatement() { Modifier = modifier, Pragmas = pragmas, Name = name.Value.ToString(), Position = name.Position };

            // base type
            Expect("An enum must have a primitive base type", TokenKind.Colon);

            if (errorsNum != Tower.Diagnostic.Count)
                return false;

            statement.BaseType = ExpectEnumBaseType();

            // enum body
            Expect(UnexpectedToken, TokenKind.OpenBrace);

            while (Match(TokenKind.Identifier))
            {
                var value = -1;
                if (statement.Body.Count > 0)
                    _ = int.TryParse(statement.Body.Last().Value.Value, out value);

                var member = ExpectMemberDefinition(statement.BaseType.UnsolvedType.Value.IsInt() || statement.BaseType.UnsolvedType.Value.Kind == TypeKind.Err, value);
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

                _pragmas.SetPragma(name, value, ParseError, Tower.TokenCollection[CurrentIndex - 3].Position);
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
                                {
                                    Report("Token out of context");
                                    CurrentIndex++; // skipping the bad token
                                }
                        }
                    }

                // adds the statement to the members
                nodes.Nodes.Add(statement);
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

            Module.Name = Token.NewInfo(TokenKind.Identifier, Tower.ModuleName);

            // search for members
            Module.Members = ExpectNamespaceMembers();
            
            // breaking the compiler workflow if diagnostic is bad
            Tower.CheckDiagnostic();

            return Module;
        }
    }
}