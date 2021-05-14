using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace Nylon.Models.Lexer
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TokenKind
    {
        [Description("bad")]
        Bad = -1,
        [Description("ident")]
        Identifier = -2,
        [Description("const string")]
        ConstantString = -3,
        [Description("const num")]
        ConstantDigit = -4,
        [Description("eof")]
        EOF = -5,
        [Description("const char")]
        ConstantChar = -6,
        [Description("func")]
        KeyFunc = -7,
        [Description("var")]
        KeyVar = -8,
        [Description("const")]
        KeyConst = -9,
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
        [Description("const float num")]
        ConstantFloatDigit = -10,
        [Description(".")]
        Dot = '.',
        [Description(",")]
        Comma = ',',
        [Description("==")]
        BooleanEQ = -1,
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
        [Description("const bool")]
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
        [Description("to")]
        KeyTo = -22,
        [Description("in")]
        KeyIn = -23,
        [Description("for")]
        KeyFor = -24,
        [Description("..")]
        RangeDots = -25,
        [Description("as")]
        KeyAs = -26,
        [Description("continue")]
        KeyContinue = -27,
        [Description("break")]
        KeyBreak = -28,
        [Description("type")]
        KeyType = -29,
        [Description("pub")]
        KeyPub = -30,
        [Description("new")]
        KeyNew = -31,
        [Description("use")]
        KeyUse = -32,
        [Description("import")]
        KeyImport = -90,
        [Description("*=")]
        MulAssignment = -33,
        [Description("/=")]
        DivAssignment = -34,
        [Description("++")]
        OperatorIncrement = -35,
        [Description("--")]
        OperatorDecrement = -36,
        [Description("|")]
        BooleanOR = '|',
        [Description("&")]
        BooleanAND = '&',
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
        KeyTry = -42
    }
}