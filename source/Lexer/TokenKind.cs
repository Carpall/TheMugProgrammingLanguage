using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace Mug.Grammar
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenKind
    {
        [Description("bad")]
        Bad = 0,
        [Description("ident")]
        Identifier = -1,
        [Description("literal string")]
        ConstantString = -2,
        [Description("literal int")]
        ConstantDigit = -3,
        [Description("eof")]
        EOF = -4,
        [Description("literal char")]
        ConstantChar = -5,
        [Description("fn")]
        KeyFunc = -6,
        [Description("const")]
        KeyConst = -8,
        [Description("(")]
        OpenPar = '(',
        [Description(")")]
        ClosePar = ')',
        [Description(":")]
        Colon = ':',
        [Description("{")]
        OpenBrace = '{',
        [Description("}")]
        CloseBrace = '}',
        [Description("[")]
        OpenBracket = '[',
        [Description("]")]
        CloseBracket = ']',
        [Description("literal float")]
        ConstantFloatDigit = -9,
        [Description(".")]
        Dot = '.',
        [Description(",")]
        Comma = ',',
        [Description("==")]
        BooleanEQ = -10,
        [Description("=")]
        Equal = '=',
        [Description("/")]
        Slash = '/',
        [Description("!=")]
        BooleanNEQ = -11,
        [Description("+=")]
        AddAssignment = -12,
        [Description("-=")]
        SubAssignment = -13,
        [Description("+")]
        Plus = '+',
        [Description("-")]
        Minus = '-',
        [Description("*")]
        Star = '*',
        [Description("return")]
        KeyReturn = -14,
        [Description("literal bool")]
        ConstantBoolean = -15,
        [Description("if")]
        KeyIf = -16,
        [Description("elif")]
        KeyElif = -17,
        [Description("else")]
        KeyElse = -18,
        [Description("<")]
        BooleanLess = '<',
        [Description(">")]
        BooleanGreater = '>',
        [Description("|")]
        Pipe = '|',
        [Description("&")]
        Apersand = '&',
        [Description("<=")]
        BooleanLEQ = -19,
        [Description(">=")]
        BooleanGEQ = -20,
        [Description("!")]
        Negation = '!',
        [Description("?")]
        QuestionMark = '?',
        [Description("while")]
        KeyWhile = -21,
        [Description("in")]
        KeyIn = -23,
        [Description("for")]
        KeyFor = -24,
        [Description("..")]
        RangeDots = -25,
        [Description("as")]
        KeyAs = -26,
        [Description("or")]
        BooleanOR = -46,
        [Description("and")]
        BooleanAND = -45,
        [Description("continue")]
        KeyContinue = -27,
        [Description("break")]
        KeyBreak = -28,
        [Description("pub")]
        KeyPub = -30,
        [Description("new")]
        KeyNew = -31,
        [Description("*=")]
        MulAssignment = -33,
        [Description("/=")]
        DivAssignment = -34,
        [Description("++")]
        OperatorIncrement = -35,
        [Description("--")]
        OperatorDecrement = -36,
        [Description("priv")]
        KeyPriv = -37,
        [Description("enum")]
        KeyEnum = -38,
        [Description("catch")]
        KeyCatch = -39,
        [Description("is")]
        KeyIs = -40,
        [Description("switch")]
        KeySwitch = -41,
        [Description("try")]
        KeyTry = -43,
        [Description("let")]
        KeyLet = -44,
        [Description("mut")]
        KeyMut = -45,
        [Description("struct")]
        KeyStruct = -46,
        [Description("static")]
        KeyStatic = -47
    }
}