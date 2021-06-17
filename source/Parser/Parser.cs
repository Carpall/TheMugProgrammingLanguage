using Mug.Compilation;
using Mug.Tokenizer;
using Mug.Parser.AST;
using Mug.Parser.AST.Directives;
using Mug.Parser.AST.Statements;
using Mug.TypeSystem;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Data;

namespace Mug.Parser
{
    public class Parser : CompilerComponent
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

        public Parser(CompilationTower tower) : base(tower)
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

        private DataType ExpectType(bool allowEnumError = false)
        {
            if (MatchAdvance(TokenKind.OpenPar, out var token, true))
                return CollectTupleType(token);
            else if (MatchAdvance(TokenKind.OpenBracket, out token, true))
                return CollectArrayType(token);
            else if (MatchAdvance(TokenKind.Star, out token, true) || MatchAdvance(TokenKind.QuestionMark, out token, true))
                return CollectPointerType(token);

            var find = ExpectBaseType();

            // struct generics
            if (MatchAdvance(TokenKind.BooleanLess, true))
                CollectGenericType(ref find);

            if (allowEnumError && MatchAdvance(TokenKind.Negation, true))
                CollectEnumErrorType(ref find);

            return find;
        }

        private void CollectEnumErrorType(ref DataType find)
        {
            find = UnsolvedType.Create(Tower, Back.Position, TypeKind.EnumError, (find, ExpectType()));
        }

        private void CollectGenericType(ref DataType find)
        {
            if (find.UnsolvedType.Kind != TypeKind.DefinedType)
                ParseError(find.Position, $"Generic parameters cannot be passed to type '{find}'");

            var genericTypes = new List<DataType>();

            do
                genericTypes.Add(ExpectType());
            while (MatchAdvance(TokenKind.Comma));

            Expect("", TokenKind.BooleanGreater);

            find = UnsolvedType.Create(
                Tower,
                new(find.Position.Lexer, find.Position.Position.Start..Back.Position.Position.End),
                TypeKind.GenericDefinedType,
                (find, genericTypes));
        }

        private DataType CollectPointerType(Token token)
        {
            var type = ExpectType();
            return UnsolvedType.Create(
                Tower,
                new(token.Position.Lexer, token.Position.Position.Start..type.UnsolvedType.Position.Position.End),
                token.Kind == TokenKind.Star ? TypeKind.Pointer : TypeKind.Option,
                type);
        }

        private DataType CollectArrayType(Token token)
        {
            var type = ExpectType();
            Expect("An array type definition must end by ']'", TokenKind.CloseBracket);
            return UnsolvedType.Create(
                Tower,
                new(token.Position.Lexer, token.Position.Position.Start..Back.Position.Position.End),
                TypeKind.Array,
                type);
        }

        private DataType CollectTupleType(Token token)
        {
            var types = new List<DataType>();
            do
                types.Add(ExpectType());
            while (MatchAdvance(TokenKind.Comma));

            Expect("", TokenKind.ClosePar);
            var position = new ModulePosition(token.Position.Lexer, token.Position.Position.Start..Back.Position.Position.End);
            return UnsolvedType.Create(Tower, position, TypeKind.Tuple, types.ToArray());
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

            var isPassedAsReference = MatchAdvance(TokenKind.Apersand);
            var name = Expect("Expected parameter's name", TokenKind.Identifier);

            if (!MatchAdvance(TokenKind.Colon))
                return new(null, name.Value, default, isPassedAsReference, name.Position);

            var type = ExpectType();
            var defaultvalue = new Token();

            if (MatchAdvance(TokenKind.Equal))
                defaultvalue = ExpectConstant("Expected constant expression as default parameter value");

            ExpectMultiple("", TokenKind.Comma, TokenKind.ClosePar);
            CurrentIndex--;

            return new(type, name.Value, defaultvalue, isPassedAsReference, name.Position);
        }

        private DataType ExpectBaseType()
        {
            if (!MatchBaseType(out var type))
                ParseError("Expected a type, but found '" + Current.Value + "'");

            return type;
        }

        private bool MatchBaseType(out DataType type)
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
                MatchSpecificIdentifier("u8")  ||
                MatchSpecificIdentifier("u16") ||
                MatchSpecificIdentifier("u32") ||
                MatchSpecificIdentifier("u64") ||
                MatchSpecificIdentifier("i8") ||
                MatchSpecificIdentifier("i16") ||
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

            e = ExpectExpression(end: TokenKind.ClosePar);

            var end = Back.Position;

            e.Position = GetModulePositionRange(start, end);
            return true;
        }

        private void CollectPossibleArrayAccessNode(ref INode e)
        {
            if (e is BadNode)
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

                name = new MemberNode()
                {
                    Base = name,
                    Member = id,
                    Position = new(name.Position.Lexer, name.Position.Position.Start.Value..id.Position.Position.End.Value)
                };

                CollectPossibleArrayAccessNode(ref name);
            }

            return true;
        }

        private NodeBuilder CollectParameters()
        {
            var parameters = new NodeBuilder();
            var lastiscomma = false;
            var start = Back.Position;

            while (!MatchAdvance(TokenKind.ClosePar))
            {
                parameters.Add(ExpectExpression(end: new[] { TokenKind.Comma, TokenKind.ClosePar}));

                lastiscomma = Back.Kind == TokenKind.Comma;
                if (Back.Kind == TokenKind.ClosePar)
                {
                    lastiscomma = false;
                    CurrentIndex--;
                }
            }

            var end = Back.Position;
            if (lastiscomma)
                Report(Back.Position, "Expected parameter's expression");

            parameters.Position = GetModulePositionRange(start, end);
            return parameters;
        }

        private List<DataType> CollectGenericParameters(ref bool builtin)
        {
            var oldindex = CurrentIndex;
            List<DataType> generics = new();

            if (MatchAdvance(TokenKind.Pipe, true))
            {
                if (builtin)
                    Report(UnexpectedToken);

                do
                    generics.Add(ExpectType());
                while (MatchAdvance(TokenKind.Comma));

                if (MatchAdvance(TokenKind.Pipe))
                {
                    builtin = CollectBuiltInSymbol();
                    return generics;
                }
            }

            CurrentIndex = oldindex;
            return generics;
        }

        private bool CollectBuiltInSymbol()
        {
            return MatchAdvance(TokenKind.Negation, true);
        }

        private bool MatchCallStatement(out INode e, INode name)
        {
            e = CreateBadNode();

            var builtin = CollectBuiltInSymbol();

            if (!Match(TokenKind.OpenPar, true) && !Match(TokenKind.Pipe, true))
                return false;
            
            var generics = CollectGenericParameters(ref builtin);

            if (!MatchAdvance(TokenKind.OpenPar, true))
            {
                if (generics.Count > 0)
                    Report("Invalid generic parameters here");

                return false;
            }

            var parameters = CollectParameters();

            e = new CallStatement()
            {
                Generics = generics,
                IsBuiltIn = builtin,
                Name = name,
                Parameters = parameters,
                Position = GetModulePositionRange(name.Position, Back.Position)
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
                var parameters = CollectParameters();

                e = new CallStatement()
                {
                    Generics = generics,
                    IsBuiltIn = builtin,
                    Position = name.Position,
                    Name = new MemberNode()
                    {
                        Base = e,
                        Member = name,
                        Position = new(e.Position.Lexer, e.Position.Position.Start..name.Position.Position.End)
                    },
                    Parameters = parameters
                };
            }
            else
            {
                if (generics.Count != 0)
                    ParseError("Expected call after generic parameter specification");

                e = new MemberNode()
                {
                    Base = e,
                    Member = name,
                    Position = new(e.Position.Lexer, e.Position.Position.Start..name.Position.Position.End)
                };
            }
        }

        private bool MatchPrefixOperator(out Token prefix)
        {
            return
                MatchAdvance(TokenKind.Minus, out prefix)             ||
                MatchAdvance(TokenKind.Plus, out prefix)              ||
                MatchAdvance(TokenKind.Negation, out prefix)          ||
                MatchAdvance(TokenKind.Apersand, out prefix)        ||
                MatchAdvance(TokenKind.Star, out prefix)              ||
                MatchAdvance(TokenKind.OperatorIncrement, out prefix) ||
                MatchAdvance(TokenKind.OperatorDecrement, out prefix);
        }

        private bool MatchTerm(out INode e, bool allowNullExpression)
        {
            if (Match(TokenKind.OpenBrace))
                return CollectBlockExpression(out e);
            else if (MatchAdvance(TokenKind.KeyTry))
                return CollectTryExpression(out e);
            else if (MatchPrefixOperator(out var prefixOP))
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
                else if (ConditionDefinition(out e))
                    return true;
            }

            if (!allowNullExpression && e is BadNode)
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
        
        private static ModulePosition GetModulePositionRange(ModulePosition left, ModulePosition right)
        {
            return new(left.Lexer, new(left.Position.Start, right.Position.End));
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
                return CollectNodeNewArray();

            // could be type inferred
            var name = ExpectTypeOr(TokenKind.OpenBrace);

            var allocation = new TypeAllocationNode() { Name = name, Position = newposition };

            Expect("Type allocation requires '{}'", TokenKind.OpenBrace);

            if (Match(TokenKind.Identifier))
                do
                    allocation.Body.Add(ExpectFieldAssign());
                while (MatchAdvance(TokenKind.Comma));

            Expect("", TokenKind.CloseBrace);

            return allocation;
        }

        private DataType ExpectTypeOr(TokenKind or)
        {
            return !Match(or) ? ExpectType() : null;
        }

        private INode CollectNodeNewArray()
        {
            var type = ExpectType();
            INode size = CreateBadNode();

            if (MatchAdvance(TokenKind.Comma))
            {
                size = ExpectExpression(end: TokenKind.CloseBracket);
                CurrentIndex--;
            }

            Expect("Expected ']' and the array body", TokenKind.CloseBracket);

            var array = new ArrayAllocationNode() { SizeIsImplicit = size is BadNode, Size = size, Type = type };
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
                    e = new BinaryExpressionNode()
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

            if (e is BadNode && !allowNullExpression)
            {
                Report($"Expected expression, found '{Current.Value}'");
                CurrentIndex++; // skipping bad token
            }

            while (allowBoolOP && MatchBooleanOperator(out var boolOP))
            {
                var boolean = new BooleanBinaryExpressionNode()
                {
                    Operator = boolOP,
                    Left = e
                };

                if (boolOP.Kind != TokenKind.KeyIs)
                {
                    boolean.Right = ExpectExpression(false, false, end: end);
                    boolean.Position = GetModulePositionRange(boolean.Left.Position, boolean.Right.Position);
                }
                else
                {
                    boolean.IsInstructionType = ExpectType();
                    if (MatchAdvance(TokenKind.Identifier, out var alias, true))
                        boolean.IsInstructionAlias = alias;

                    boolean.Position = GetModulePositionRange(boolean.Left.Position, boolean.IsInstructionType.Position);
                }

                e = boolean;
            }

            while (allowLogicOP && MatchAndOrOperator())
            {
                var op = Back;
                var right = ExpectExpression(allowLogicOP: false, end: end);
                e = new BooleanBinaryExpressionNode()
                {
                    Operator = op,
                    Left = e,
                    Right = right,
                    Position = GetModulePositionRange(e.Position, right.Position)
                };
            }

            if (MatchAssigmentOperators(out var @operator))
                e = new AssignmentStatement()
                {
                    Name = e,
                    Operator = @operator,
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

        private DataType ExpectVariableType()
        {
            return MatchAdvance(TokenKind.Colon) ? ExpectType() : UnsolvedType.Automatic(Tower, Back.Position);
        }

        private bool VariableDefinition(out INode statement)
        {
            statement = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyVar, out var token) && !MatchAdvance(TokenKind.KeyConst, out token))
                return false;

            var name = Expect("Expected the variable name", TokenKind.Identifier);
            var type = ExpectVariableType();
            var body = MatchAdvance(TokenKind.Equal) ? ExpectExpression(true) : CreateBadNode();

            statement = new VariableStatement()
            {
                Body = body,
                Name = name.Value.ToString(),
                Position = name.Position,
                Type = type,
                IsConst = token.Kind == TokenKind.KeyConst
            };

            return true;
        }

        private bool NextIsOnSameLine()
        {
            return CurrentIndex < Tower.TokenCollection.Count && !Tower.TokenCollection[CurrentIndex].IsOnNewLine;
        }

        private bool ReturnDeclaration(out INode statement)
        {
            statement = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyReturn))
                return false;

            var pos = Back.Position;

            statement = new ReturnStatement()
            {
                Position = pos,
                Body = !NextIsOnSameLine() ? CreateBadNode() : ExpectExpression()
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
                var expression = (INode)CreateBadNode();
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

        private bool ForLoopDefinition(out INode statement)
        {
            statement = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyFor, out var key))
                return false;

            var leftexpr = ExpectStatement(true, true);
            Expect("", TokenKind.Comma);
            var conditionexpr = ExpectExpression(allowNullExpression: true, end: TokenKind.Comma);
            var rightexpr = ExpectStatement(true, true);

            // CurrentIndex--; // returning on '{' token

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
            statement = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyContinue) &&
                !MatchAdvance(TokenKind.KeyBreak))
                return false;

            statement = new LoopManagementStatement() { Kind = Back.Kind, Position = Back.Position };

            return true;
        }

        private INode ExpectStatement(bool isfirst, bool allowNull = false)
        {
            var multipleStatementsOnTheSameLine = !isfirst && !Current.IsOnNewLine;

            if (!VariableDefinition(out var statement) && // var x = value;
                !ReturnDeclaration(out statement) && // return value;
                !ForLoopDefinition(out statement) && // for x: type to, in value {}
                !LoopManagerDefintion(out statement)) // continue, break
                statement = ExpectExpression(true, allowNullExpression: allowNull);

            if (multipleStatementsOnTheSameLine)
                Tower.Warn(statement.Position, "Maintain statements on different lines");

            return statement;
        }

        private BlockNode ExpectBlock()
        {
            var start = Expect("", TokenKind.OpenBrace).Position;
            var block = new BlockNode();
            var i = 0;

            while (!Match(TokenKind.CloseBrace))
            {
                var statement = ExpectStatement(i++ == 0);
                if (statement is not BadNode)
                    block.Statements.Add(statement);
            }

            var end = Expect("", TokenKind.CloseBrace).Position;

            block.Position = GetModulePositionRange(start, end);
            return block;
        }

        private ParameterNode[] ExpectParameterListDeclaration()
        {
            Expect("", TokenKind.OpenPar);

            var parameters = new List<ParameterNode>();
            var count = 0;

            while (!MatchAdvance(TokenKind.ClosePar))
            {
                parameters.Add(ExpectParameter(count == 0));
                count++;
            }

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
                parameters[i].Type = DataType.Undefined;
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
            var type = parameters[index].Type;
            var parametersCount = index - i;

            while (parametersCount-- >= 0)
            {
                parameters[i].Type = type;
                i++;
            }
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
            return result ?? new();
        }

        private bool FunctionDefinition(out INode node)
        {
            node = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyFunc)) // <func>
                return false;

            var modifier = GetModifier();
            var pragmas = GetPramas();
            var generics = new List<Token>();

            var name = Expect("In function definition must specify the name", TokenKind.Identifier); // func <name>

            CollectGenericParameterDefinitions(generics);

            var parameters = ExpectParameterListDeclaration(); // func name<(..)>

            DataType type =
                MatchAdvance(TokenKind.Colon) ?
                    ExpectType(true) :
                    UnsolvedType.Create(Tower, name.Position, TypeKind.Void);

            var prototype = new FunctionStatement
            {
                Modifier = modifier,
                Pragmas = pragmas,
                Name = name.Value.ToString(),
                ParameterList = parameters,
                ReturnType = type,
                Position = name.Position,
                Generics = generics,
            };

            if (Match(TokenKind.OpenBrace)) // function definition
                prototype.Body = ExpectBlock();

            node = prototype;
            return true;
        }

        private bool MatchImportDirective(out INode directive)
        {
            directive = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyImport, out var token)) // <import>
                return false;

            INode body;

            var mode = ImportMode.FromPackages;

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
            directive = CreateBadNode();

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
                return new FieldNode { Name = "", Type = DataType.Void };
            }

            var type = ExpectType(); // field: <error>
            EatComma();

            return new FieldNode()
            {
                Pragmas = GetPramas(),
                Modifier = GetModifier(),
                Name = name.Value.ToString(),
                Type = type,
                Position = name.Position
            };
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
            node = CreateBadNode();
            var variant = new VariantStatement()
            {
                Pragmas = pragmas,
                Modifier = modifier,
                Name = name.Value,
                Position = name.Position
            };

            Expect("", TokenKind.OpenPar);

            if (MatchAdvance(TokenKind.ClosePar))
                return;

            do
                variant.Body.Add(ExpectType());
            while (MatchAdvance(TokenKind.BooleanOR));

            Expect("", TokenKind.ClosePar);

            node = variant;
        }

        /// <summary>
        /// search for a struct definition
        /// </summary>
        private bool TypeDefinition(out INode node)
        {
            node = CreateBadNode();

            // returns if does not match a type keyword
            if (!MatchAdvance(TokenKind.KeyType))
                return false;

            // required an identifier
            var modifier = GetModifier();
            var pragmas = GetPramas();
            var name = Expect("Expected the type name after 'type' keyword", TokenKind.Identifier);
            node = new TypeStatement()
            {
                Modifier = modifier,
                Pragmas = pragmas,
                Name = name.Value.ToString(),
                Position = name.Position
            };

            // struct generics
            CollectGenericParameterDefinitions((node as TypeStatement).Generics);

            // variant definition
            if (MatchAdvance(TokenKind.Equal))
                ExpectVariantDefinition(out node, name, pragmas, modifier);
            else
            {
                // struct body
                Expect(UnexpectedToken, TokenKind.OpenBrace);
                CollectTypeBody(node as TypeStatement);
            }

            return true;
        }

        private void CollectTypeBody(TypeStatement statement)
        {
            while (!MatchAdvance(TokenKind.CloseBrace))
            {
                CollectModifier();
                CollectPragmas();

                if (FunctionDefinition(out var node))
                    statement.BodyMethods.Add(node as FunctionStatement);
                else
                    statement.BodyFields.Add(ExpectFieldDefinition());
            }
        }

        private VariableStatement CheckConstInTypeDefinitionAndEatComma(INode opaque)
        {
            var node = opaque as VariableStatement;
            if (!node.IsConst)
                Report(opaque.Position, "Expected keyword 'const' not 'var'");

            EatComma();

            return node;
        }

        private EnumMemberNode ExpectMemberDefinition(bool basetypeisint, int lastvalue)
        {
            var name = Expect("Expected enum's member name", TokenKind.Identifier);
            EatComma();
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

        private void EatComma()
        {
            MatchAdvance(TokenKind.Comma);
        }

        private DataType ExpectPrimitiveType(bool allowErr)
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
                return false;
        }

        private DataType ExpectEnumBaseType()
        {
            if (!MatchAdvance(TokenKind.OpenPar))
                return UnsolvedType.Create(Tower, Back.Position, TypeKind.UInt8);

            var type = ExpectPrimitiveType(true);
            Expect("", TokenKind.ClosePar);
            return type;
        }

        private bool EnumDefinition(out INode node)
        {
            node = CreateBadNode();

            if (!MatchAdvance(TokenKind.KeyEnum))
                return false;

            var name = Expect("Expected the type name after 'enum' keyword", TokenKind.Identifier);
            var statement = new EnumStatement()
            {
                Modifier = GetModifier(),
                Pragmas = GetPramas(),
                Name = name.Value.ToString(),
                Position = name.Position
            };

            statement.BaseType = ExpectEnumBaseType();

            // enum body
            CollectEnumBody(statement);

            node = statement;

            return true;
        }

        private void CollectEnumBody(EnumStatement statement)
        {
            Expect(UnexpectedToken, TokenKind.OpenBrace);

            while (Match(TokenKind.Identifier))
            {
                var value = -1;
                if (statement.Body.Count > 0)
                    _ = int.TryParse(statement.Body.Last().Value.Value, out value);

                var member = ExpectMemberDefinition(
                    statement.BaseType.UnsolvedType.IsInt() || statement.BaseType.UnsolvedType.Kind == TypeKind.Err,
                    value);

                statement.Body.Add(member);
            }

            Expect(UnexpectedToken, TokenKind.CloseBrace); // expected close body
        }

        private void CollectPragmas()
        {
            if (!MatchAdvance(TokenKind.OpenBracket))
                return;

            _pragmas = new();

            do
            {
                var name = Expect("Expected pragma's name", TokenKind.Identifier);
                var value = MatchAdvance(TokenKind.Colon) ?
                    ExpectConstant("Non-constant expressions not allowed in pragmas") : Token.NewInfo(TokenKind.ConstantBoolean, "true");

                _pragmas.SetPragma(name.Value, value, Tower, name.Position);
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
                if (!FunctionDefinition(out var statement))
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
                                    continue;
                                }
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

            Module.Name = Token.NewInfo(TokenKind.Identifier, Tower.OutputFilename);

            // search for members
            Module.Members = ExpectNamespaceMembers();
            
            // breaking the compiler workflow if diagnostic is bad
            // Tower.CheckDiagnostic();

            return Module;
        }
    }
}