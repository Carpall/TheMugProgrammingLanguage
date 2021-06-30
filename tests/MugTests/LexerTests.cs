using Mug.Compilation;
using Mug.Grammar;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Mug.Tests
{
    public class LexerTests
    {
        // Well constructed code strings
        private const string OPERATION01 = "1 + 2";
        private const string VARIABLE01 = "let x = 0 ";
        private const string VARIABLE02 = "let number: i32 = 50 ";

        private const string COMMENTS01 = "//This is a comment";
        private const string COMMENTS02 = "/* This is a  multi-line comment */";

        private const string SINGLE_TOKENS = "( ) [ ] { } < > = ! & | + - * / ,   : .  ";
        private const string DOUBLE_TOKENS = "== != ++ += -- -= *= /= <= >= ..";
        private const string FULL_TOKENS = "return continue break while pub use import new for      as in to if elif else fn   let const str chr       i32 i64 u8 u32 u64 unknown when declare void bool";
        private const string RANDOM_TOKENS = "return == ( ) += continue pub ! *= ..";


        private const string STRINGS01 = "\"This is a string\"";

        //Ill-constructed code strings
        private const string EMPTYSTRING = "";

        private const string STRINGS02 = "\"This is a non-closed string";
        private const string STRINGS03 = "\"This is a \" nested \"string\"";
        private const string STRINGS04 = "\"\\n\\t\\r\\\"\"";
        private const string STRINGS05 = "i32\"\\\\ \"t token";
        private const string STRINGS06 = "\"";

        private const string CHARS01 = "'c'";
        private const string CHARS02 = "'c";
        private const string CHARS03 = "'cc'";
        private const string CHARS04 = "'\\\\'";
        private const string CHARS05 = "'\\\\\\\\'";

        private const string BACKTICKS01 = "`*`";
        private const string BACKTICKS02 = "`/";
        private const string BACKTICKS03 = "``";

        private const string VARIABLE03 = " 50 = i32 :number let";
        private const string VARIABLE04 = "varnumber";
        private const string VARIABLE05 = "i33";

        private const string COMMENTS03 = "/* This is a non closed multi-line comment";
        private const string COMMENTS04 = "/* This is a nested */ multi-line comment */";
        private const string COMMENTS05 = "/* This is a /* nested */ multi-line comment */";

        private static Lexer NewLexer(string test)
        {
            var lexer = new Lexer(new("test", new()));
            lexer.SetSource(new Source("test", test));
            return lexer;
        }

        [Test]
        public void GetLength_EmptyCollection_ReturnZero()
        {
            var lexer = NewLexer(OPERATION01);

            Assert.AreEqual(lexer.Length, 0);
        }

        [Test]
        public void GetLength_NonEmptyCollection_ReturnLength()
        {
            var lexer = NewLexer(VARIABLE01);
            lexer.Tokenize();

            Assert.AreEqual(lexer.Length, 5);

            Console.WriteLine($"0: {lexer.TokenCollection[0].Value}");
            Console.WriteLine($"1: {lexer.TokenCollection[1].Value}");
            Console.WriteLine($"2: {lexer.TokenCollection[2].Value}");
            Console.WriteLine($"3: {lexer.TokenCollection[3].Value}");
            Console.WriteLine($"4: {lexer.TokenCollection[4].Value}");
        }

        public void AreListEqual(List<Token> expected, List<Token> reals)
        {
            if (reals.Count != expected.Count)
            {
                Console.WriteLine("expected contained:");
                for (var i = 0; i < expected.Count; i++)
                    Console.WriteLine($"i:{i}, contained:{expected[i]}");
                Console.WriteLine("reals contained:");
                for (var i = 0; i < reals.Count; i++)
                    Console.WriteLine($"i:{i}, contained:{reals[i]}");
                Assert.Fail($"Assert different lenghts:\n   - expected {expected.Count} tokens\n   - found {reals.Count} tokens");
            }

            for (var i = 0; i < reals.Count; i++)
                if (!reals[i].Equals(expected[i]))
                    Assert.Fail($"Assert different values:\n   - expected: {expected[i]}\n   - found: {reals[i]}");

            Assert.Pass();
        }

        [Test]
        public void Test01_CorrectTokenization()
        {
            var lexer = NewLexer(VARIABLE01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.KeyLet, "let", new(lexer.Source, 0..3)),
                Token.NewInfo(TokenKind.Identifier, "x", new(lexer.Source, 4..5)),
                Token.NewInfo(TokenKind.Equal, "=", new(lexer.Source, 6..7)),
                Token.NewInfo(TokenKind.ConstantDigit, "0", new(lexer.Source, 8..9)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 10..11))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test02_CorrectTokenization()
        {
            var lexer = NewLexer(VARIABLE02);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.KeyLet, "let", new(lexer.Source, 0..3)),
                Token.NewInfo(TokenKind.Identifier, "number", new(lexer.Source, 4..10)),
                Token.NewInfo(TokenKind.Colon, ":", new(lexer.Source, 10..11)),
                Token.NewInfo(TokenKind.Identifier, "i32", new(lexer.Source, 12..15)),
                Token.NewInfo(TokenKind.Equal, "=", new(lexer.Source, 16..17)),
                Token.NewInfo(TokenKind.ConstantDigit, "50", new(lexer.Source, 18..20)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 21..22))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test03_CorrectTokenization()
        {
            var lexer = NewLexer(VARIABLE03);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.ConstantDigit, "50", new(lexer.Source, 1..3)),
                Token.NewInfo(TokenKind.Equal, "=", new(lexer.Source, 4..5)),
                Token.NewInfo(TokenKind.Identifier, "i32", new(lexer.Source, 6..9)),
                Token.NewInfo(TokenKind.Colon, ":", new(lexer.Source, 10..11)),
                Token.NewInfo(TokenKind.Identifier, "number", new(lexer.Source, 11..17)),
                Token.NewInfo(TokenKind.KeyLet, "let", new(lexer.Source, 18..21)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 21..22))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test04_CorrectTokenization()
        {
            var lexer = NewLexer(VARIABLE04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.Identifier, "varnumber", new(lexer.Source, 0..9)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 9..10))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test05_CorrectTokenization()
        {
            var lexer = NewLexer(VARIABLE05);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.Identifier, "i33", new(lexer.Source, 0..3)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 3..4))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments01_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = NewLexer(COMMENTS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 20..21))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments02_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = NewLexer(COMMENTS02);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 36..37))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments03_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = NewLexer(COMMENTS03);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 43..44))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments04_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = NewLexer(COMMENTS04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.Identifier, "multi", new(lexer.Source, 23..28)),
                Token.NewInfo(TokenKind.Minus, "-", new(lexer.Source, 28..29)),
                Token.NewInfo(TokenKind.Identifier, "line", new(lexer.Source, 29..33)),
                Token.NewInfo(TokenKind.Identifier, "comment", new(lexer.Source, 34..41)),
                Token.NewInfo(TokenKind.Star, "*", new(lexer.Source, 42..43)),
                Token.NewInfo(TokenKind.Slash, "/", new(lexer.Source, 43..44)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 44..45))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments05_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = NewLexer(COMMENTS05);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.Identifier, "multi", new(lexer.Source, 26..31)),
                Token.NewInfo(TokenKind.Minus, "-", new(lexer.Source, 31..32)),
                Token.NewInfo(TokenKind.Identifier, "line", new(lexer.Source, 32..36)),
                Token.NewInfo(TokenKind.Identifier, "comment", new(lexer.Source, 37..44)),
                Token.NewInfo(TokenKind.Star, "*", new(lexer.Source, 45..46)),
                Token.NewInfo(TokenKind.Slash, "/", new(lexer.Source, 46..47)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 47..48))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void EmptyString_CorrectTokenization()
        {
            // An empty string gets converted into an <EOF>
            var lexer = NewLexer(EMPTYSTRING);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 0..1))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings01_CorrectTokenization()
        {
            var lexer = NewLexer(STRINGS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.ConstantString, "This is a string", new(lexer.Source, 0..18)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 18..19))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings02_ExceptionCaught()
        {
            var lexer = NewLexer(STRINGS02);
            lexer.Tokenize();

            Assert.AreEqual("Constant string has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestStrings03_CorrectTokenization()
        {
            var lexer = NewLexer(STRINGS03);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.ConstantString, "This is a ", new(lexer.Source, 0..12)),
                Token.NewInfo(TokenKind.Identifier, "nested", new(lexer.Source, 13..19)),
                Token.NewInfo(TokenKind.ConstantString, "string", new(lexer.Source, 20..28)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 28..29))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings04_EscapedChars()
        {
            var lexer = NewLexer(STRINGS04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.ConstantString, "\n\t\r\"", new(lexer.Source, 0..10)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 10..11))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings05_AdvancedEscapedChars()
        {
            var lexer = NewLexer(STRINGS05);
            lexer.Tokenize();
            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.Identifier, "i32", new(lexer.Source, 0 ..3)),
                Token.NewInfo(TokenKind.ConstantString, "\\ ", new(lexer.Source, 3 ..8)),
                Token.NewInfo(TokenKind.Identifier, "t", new(lexer.Source, 8 ..9)),
                Token.NewInfo(TokenKind.Identifier, "token", new(lexer.Source, 10..15)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 15 ..16))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings06_ExceptionCaught()
        {
            var lexer = NewLexer(STRINGS06);
            lexer.Tokenize();

            Assert.AreEqual("String has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestSingleTokens_CorrectTokenization()
        {
            var lexer = NewLexer(SINGLE_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.OpenPar, "(", new(lexer.Source, 0..1)),
                Token.NewInfo(TokenKind.ClosePar, ")", new(lexer.Source, 2..3)),
                Token.NewInfo(TokenKind.OpenBracket, "[", new(lexer.Source, 4..5)),
                Token.NewInfo(TokenKind.CloseBracket, "]", new(lexer.Source, 6..7)),
                Token.NewInfo(TokenKind.OpenBrace, "{", new(lexer.Source, 8..9)),
                Token.NewInfo(TokenKind.CloseBrace, "}", new(lexer.Source, 10..11)),
                Token.NewInfo(TokenKind.BooleanLess, "<", new(lexer.Source, 12..13)),
                Token.NewInfo(TokenKind.BooleanGreater, ">", new(lexer.Source, 14..15)),
                Token.NewInfo(TokenKind.Equal, "=", new(lexer.Source, 16..17)),
                Token.NewInfo(TokenKind.Negation, "!", new(lexer.Source, 18..19)),
                Token.NewInfo(TokenKind.Apersand, "&", new(lexer.Source, 20..21)),
                Token.NewInfo(TokenKind.Pipe, "|", new(lexer.Source, 22..23)),
                Token.NewInfo(TokenKind.Plus, "+", new(lexer.Source, 24..25)),
                Token.NewInfo(TokenKind.Minus, "-", new(lexer.Source, 26..27)),
                Token.NewInfo(TokenKind.Star, "*", new(lexer.Source, 28..29)),
                Token.NewInfo(TokenKind.Slash, "/", new(lexer.Source, 30..31)),
                Token.NewInfo(TokenKind.Comma, ",", new(lexer.Source, 32..33)),
                Token.NewInfo(TokenKind.Colon, ":", new(lexer.Source, 36..37)),
                Token.NewInfo(TokenKind.Dot, ".", new(lexer.Source, 38..39)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 41..42))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestDoubleTokens_CorrectTokenization()
        {
            var lexer = NewLexer(DOUBLE_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.BooleanEQ, "==", new(lexer.Source, 0..2)),
                Token.NewInfo(TokenKind.BooleanNEQ, "!=", new(lexer.Source, 3..5)),
                Token.NewInfo(TokenKind.OperatorIncrement, "++", new(lexer.Source, 6..8)),
                Token.NewInfo(TokenKind.AddAssignment, "+=", new(lexer.Source, 9..11)),
                Token.NewInfo(TokenKind.OperatorDecrement, "--", new(lexer.Source, 12..14)),
                Token.NewInfo(TokenKind.SubAssignment, "-=", new(lexer.Source, 15..17)),
                Token.NewInfo(TokenKind.MulAssignment, "*=", new(lexer.Source, 18..20)),
                Token.NewInfo(TokenKind.DivAssignment, "/=", new(lexer.Source, 21..23)),
                Token.NewInfo(TokenKind.BooleanLEQ, "<=", new(lexer.Source, 24..26)),
                Token.NewInfo(TokenKind.BooleanGEQ, ">=", new(lexer.Source, 27..29)),
                Token.NewInfo(TokenKind.RangeDots, "..", new(lexer.Source, 30..32)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 32..33))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestFullTokens_CorrectTokenization()
        {
            var lexer = NewLexer(FULL_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.KeyReturn, "return", new(lexer.Source, 0..6)),
                Token.NewInfo(TokenKind.KeyContinue, "continue", new(lexer.Source, 7..15)),
                Token.NewInfo(TokenKind.KeyBreak, "break", new(lexer.Source, 16..21)),
                Token.NewInfo(TokenKind.KeyWhile, "while", new(lexer.Source, 22..27)),
                Token.NewInfo(TokenKind.KeyPub, "pub", new(lexer.Source, 28..31)),
                Token.NewInfo(TokenKind.KeyUse, "use", new(lexer.Source, 32..35)),
                Token.NewInfo(TokenKind.KeyImport, "import", new(lexer.Source, 36..42)),
                Token.NewInfo(TokenKind.KeyNew, "new", new(lexer.Source, 43..46)),
                Token.NewInfo(TokenKind.KeyFor, "for", new(lexer.Source, 47..50)),
                Token.NewInfo(TokenKind.KeyAs, "as", new(lexer.Source, 56..58)),
                Token.NewInfo(TokenKind.KeyIn, "in", new(lexer.Source, 59..61)),
                Token.NewInfo(TokenKind.Identifier, "to", new(lexer.Source, 62..64)),
                Token.NewInfo(TokenKind.KeyIf, "if", new(lexer.Source, 65..67)),
                Token.NewInfo(TokenKind.KeyElif, "elif", new(lexer.Source, 68..72)),
                Token.NewInfo(TokenKind.KeyElse, "else", new(lexer.Source, 73..77)),
                Token.NewInfo(TokenKind.KeyFunc, "fn", new(lexer.Source, 78..80)),
                Token.NewInfo(TokenKind.KeyLet, "let", new(lexer.Source, 83..86)),
                Token.NewInfo(TokenKind.KeyConst, "const", new(lexer.Source, 87..92)),
                Token.NewInfo(TokenKind.Identifier, "str", new(lexer.Source, 93..96)),
                Token.NewInfo(TokenKind.Identifier, "chr", new(lexer.Source, 97..100)),
                Token.NewInfo(TokenKind.Identifier, "i32", new(lexer.Source, 107..110)),
                Token.NewInfo(TokenKind.Identifier, "i64", new(lexer.Source, 111..114)),
                Token.NewInfo(TokenKind.Identifier, "u8", new(lexer.Source, 115..117)),
                Token.NewInfo(TokenKind.Identifier, "u32", new(lexer.Source, 118..121)),
                Token.NewInfo(TokenKind.Identifier, "u64", new(lexer.Source, 122..125)),
                Token.NewInfo(TokenKind.Identifier, "unknown", new(lexer.Source, 126..133)),
                Token.NewInfo(TokenKind.Identifier, "when", new(lexer.Source, 134..138)),
                Token.NewInfo(TokenKind.Identifier, "declare", new(lexer.Source, 139..146)),
                Token.NewInfo(TokenKind.Identifier, "void", new(lexer.Source, 147..151)),
                Token.NewInfo(TokenKind.Identifier, "bool", new(lexer.Source, 152..156)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 156..157))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestRandomTokens_CorrectTokenization()
        {
            var lexer = NewLexer(RANDOM_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.KeyReturn, "return", new(lexer.Source, 0..6)),
                Token.NewInfo(TokenKind.BooleanEQ, "==", new(lexer.Source, 7..9)),
                Token.NewInfo(TokenKind.OpenPar, "(", new(lexer.Source, 10..11)),
                Token.NewInfo(TokenKind.ClosePar, ")", new(lexer.Source, 12..13)),
                Token.NewInfo(TokenKind.AddAssignment, "+=", new(lexer.Source, 14..16)),
                Token.NewInfo(TokenKind.KeyContinue, "continue", new(lexer.Source, 17..25)),
                Token.NewInfo(TokenKind.KeyPub, "pub", new(lexer.Source, 26..29)),
                Token.NewInfo(TokenKind.Negation, "!", new(lexer.Source, 30..31)),
                Token.NewInfo(TokenKind.MulAssignment, "*=", new(lexer.Source, 32..34)),
                Token.NewInfo(TokenKind.RangeDots, "..", new(lexer.Source, 35..37)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 37..38))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars01_OneChar()
        {
            var lexer = NewLexer(CHARS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.ConstantChar, "c", new(lexer.Source, 0..3)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 3..4))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars02_OneChar()
        {
            var lexer = NewLexer(CHARS02);
            lexer.Tokenize();

            Assert.AreEqual("Constant char has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestChars03_TooManyChars()
        {
            var lexer = NewLexer(CHARS03);
            lexer.Tokenize();

            Assert.AreEqual("Too many characters in const char", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestChars04_OneEscapedChar()
        {
            var lexer = NewLexer(CHARS04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.ConstantChar, "\\", new(lexer.Source, 0..4)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 4..5))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars05_TooManyEscapedChars()
        {
            var lexer = NewLexer(CHARS05);
            lexer.Tokenize();

            Assert.AreEqual("Too many characters in const char", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestBackticks01_OneSymbol()
        {
            var lexer = NewLexer(BACKTICKS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                Token.NewInfo(TokenKind.Identifier, "*", new(lexer.Source, 0..3)),
                Token.NewInfo(TokenKind.EOF, "<EOF>", new(lexer.Source, 3..4))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestBackticks02_OneChar()
        {
            var lexer = NewLexer(BACKTICKS02);
            lexer.Tokenize();

            Assert.AreEqual("Backtick sequence has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestBackticks03_Empty()
        {
            var lexer = NewLexer(BACKTICKS03);
            lexer.Tokenize();

            Assert.AreEqual("Not enough characters in backtick sequence", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }
    }
}
