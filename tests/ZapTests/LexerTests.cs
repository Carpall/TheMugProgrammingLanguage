using Zap.Compilation;
using Zap.Models.Lexer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zap.Tests
{
    public class LexerTests
    {
        // Well constructed code strings
        private const string OPERATION01 = "1 + 2";
        private const string VARIABLE01 = "var x = 0 ";
        private const string VARIABLE02 = "var number: i32 = 50 ";

        private const string COMMENTS01 = "//This is a comment";
        private const string COMMENTS02 = "/* This is a  multi-line comment */";

        private const string SINGLE_TOKENS = "( ) [ ] { } < > = ! & | + - * / ,   : .  ";
        private const string DOUBLE_TOKENS = "== != ++ += -- -= *= /= <= >= ..";
        private const string FULL_TOKENS = "return continue break while pub use import new for type as in to if elif else func var const str chr       i32 i64 u8 u32 u64 unknown when declare void bool";
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

        private const string VARIABLE03 = " 50 = i32 :number var";
        private const string VARIABLE04 = "varnumber";
        private const string VARIABLE05 = "i33";

        private const string COMMENTS03 = "/* This is a non closed multi-line comment";
        private const string COMMENTS04 = "/* This is a nested */ multi-line comment */";
        private const string COMMENTS05 = "/* This is a /* nested */ multi-line comment */";

        [Test]
        public void GetLength_EmptyCollection_ReturnZero()
        {
            Lexer lexer = new Lexer("test", OPERATION01);

            Assert.AreEqual(lexer.Length, 0);
        }

        [Test]
        public void GetLength_NonEmptyCollection_ReturnLength()
        {
            Lexer lexer = new Lexer("test", VARIABLE01);
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
                for (int i = 0; i < expected.Count; i++)
                    Console.WriteLine($"i:{i}, contained:{expected[i]}");
                Console.WriteLine("reals contained:");
                for (int i = 0; i < reals.Count; i++)
                    Console.WriteLine($"i:{i}, contained:{reals[i]}");
                Assert.Fail($"Assert different lenghts:\n   - expected {expected.Count} tokens\n   - found {reals.Count} tokens");
            }

            for (int i = 0; i < reals.Count; i++)
                if (!reals[i].Equals(expected[i]))
                    Assert.Fail($"Assert different values:\n   - expected: {expected[i]}\n   - found: {reals[i]}");

            Assert.Pass();
        }

        [Test]
        public void Test01_CorrectTokenization()
        {
            Lexer lexer = new Lexer("test", VARIABLE01);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.KeyVar, "var", new(lexer, 0..3)),
                new Token(TokenKind.Identifier, "x", new(lexer, 4..5)),
                new Token(TokenKind.Equal, "=", new(lexer, 6..7)),
                new Token(TokenKind.ConstantDigit, "0", new(lexer, 8..9)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 10..11))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test02_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", VARIABLE02);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.KeyVar, "var", new(lexer, 0..3)),
                new Token(TokenKind.Identifier, "number", new(lexer, 4..10)),
                new Token(TokenKind.Colon, ":", new(lexer, 10..11)),
                new Token(TokenKind.KeyTi32, "i32", new(lexer, 12..15)),
                new Token(TokenKind.Equal, "=", new(lexer, 16..17)),
                new Token(TokenKind.ConstantDigit, "50", new(lexer, 18..20)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 21..22))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test03_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", VARIABLE03);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.ConstantDigit, "50", new(lexer, 1..3)),
                new Token(TokenKind.Equal, "=", new(lexer, 4..5)),
                new Token(TokenKind.KeyTi32, "i32", new(lexer, 6..9)),
                new Token(TokenKind.Colon, ":", new(lexer, 10..11)),
                new Token(TokenKind.Identifier, "number", new(lexer, 11..17)),
                new Token(TokenKind.KeyVar, "var", new(lexer, 18..21)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 21..22))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test04_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", VARIABLE04);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.Identifier, "varnumber", new(lexer, 0..9)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 9..10))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test05_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", VARIABLE05);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.Identifier, "i33", new(lexer, 0..3)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 3..4))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments01_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            Lexer lexer = new MugLexer("test", COMMENTS01);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 20..21))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments02_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            Lexer lexer = new MugLexer("test", COMMENTS02);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 36..37))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments03_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            Lexer lexer = new MugLexer("test", COMMENTS03);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 43..44))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments04_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            Lexer lexer = new MugLexer("test", COMMENTS04);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.Identifier, "multi", new(lexer, 23..28)),
                new Token(TokenKind.Minus, "-", new(lexer, 28..29)),
                new Token(TokenKind.Identifier, "line", new(lexer, 29..33)),
                new Token(TokenKind.Identifier, "comment", new(lexer, 34..41)),
                new Token(TokenKind.Star, "*", new(lexer, 42..43)),
                new Token(TokenKind.Slash, "/", new(lexer, 43..44)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 44..45))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments05_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            Lexer lexer = new MugLexer("test", COMMENTS05);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.Identifier, "multi", new(lexer, 26..31)),
                new Token(TokenKind.Minus, "-", new(lexer, 31..32)),
                new Token(TokenKind.Identifier, "line", new(lexer, 32..36)),
                new Token(TokenKind.Identifier, "comment", new(lexer, 37..44)),
                new Token(TokenKind.Star, "*", new(lexer, 45..46)),
                new Token(TokenKind.Slash, "/", new(lexer, 46..47)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 47..48))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void EmptyString_CorrectTokenization()
        {
            // An empty string gets converted into an <EOF>
            Lexer lexer = new MugLexer("test", EMPTYSTRING);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 0..1))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings01_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", STRINGS01);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.ConstantString, "This is a string", new(lexer, 0..18)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 18..19))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings02_ExceptionCaught()
        {
            Lexer lexer = new MugLexer("test", STRINGS02);
            var ex = Assert.Throws<Zap.Compilation.CompilationException>(() => lexer.Tokenize());

            Assert.AreEqual("String has not been correctly enclosed", ex.Diagnostic.GetErrors().First().Message);
        }

        [Test]
        public void TestStrings03_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", STRINGS03);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.ConstantString, "This is a ", new(lexer, 0..12)),
                new Token(TokenKind.Identifier, "nested", new(lexer, 13..19)),
                new Token(TokenKind.ConstantString, "string", new(lexer, 20..28)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 28..29))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings04_EscapedChars()
        {
            Lexer lexer = new MugLexer("test", STRINGS04);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.ConstantString, "\n\t\r\"", new(lexer, 0..10)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 10..11))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings05_AdvancedEscapedChars()
        {
            Lexer lexer = new MugLexer("test", STRINGS05);
            lexer.Tokenize();
            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.KeyTi32, "i32", new(lexer, 0..3)),
                new Token(TokenKind.ConstantString, "\\ ", new(lexer, 3..8)),
                new Token(TokenKind.Identifier, "t", new(lexer, 8..9)),
                new Token(TokenKind.Identifier, "token", new(lexer, 10..15)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 15..16))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings06_ExceptionCaught()
        {
            Lexer lexer = new MugLexer("test", STRINGS06);
            var ex = Assert.Throws<CompilationException>(() => lexer.Tokenize());

            Assert.AreEqual("String has not been correctly enclosed", ex.Diagnostic.GetErrors().First().Message);
        }

        [Test]
        public void TestSingleTokens_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", SINGLE_TOKENS);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.OpenPar, "(", new(lexer, 0..1)),
                new Token(TokenKind.ClosePar, ")", new(lexer, 2..3)),
                new Token(TokenKind.OpenBracket, "[", new(lexer, 4..5)),
                new Token(TokenKind.CloseBracket, "]", new(lexer, 6..7)),
                new Token(TokenKind.OpenBrace, "{", new(lexer, 8..9)),
                new Token(TokenKind.CloseBrace, "}", new(lexer, 10..11)),
                new Token(TokenKind.BooleanLess, "<", new(lexer, 12..13)),
                new Token(TokenKind.BooleanGreater, ">", new(lexer, 14..15)),
                new Token(TokenKind.Equal, "=", new(lexer, 16..17)),
                new Token(TokenKind.Negation, "!", new(lexer, 18..19)),
                new Token(TokenKind.BooleanAND, "&", new(lexer, 20..21)),
                new Token(TokenKind.BooleanOR, "|", new(lexer, 22..23)),
                new Token(TokenKind.Plus, "+", new(lexer, 24..25)),
                new Token(TokenKind.Minus, "-", new(lexer, 26..27)),
                new Token(TokenKind.Star, "*", new(lexer, 28..29)),
                new Token(TokenKind.Slash, "/", new(lexer, 30..31)),
                new Token(TokenKind.Comma, ",", new(lexer, 32..33)),
                new Token(TokenKind.Colon, ":", new(lexer, 36..37)),
                new Token(TokenKind.Dot, ".", new(lexer, 38..39)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 41..42))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestDoubleTokens_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", DOUBLE_TOKENS);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.BooleanEQ, "==", new(lexer, 0..2)),
                new Token(TokenKind.BooleanNEQ, "!=", new(lexer, 3..5)),
                new Token(TokenKind.OperatorIncrement, "++", new(lexer, 6..8)),
                new Token(TokenKind.AddAssignment, "+=", new(lexer, 9..11)),
                new Token(TokenKind.OperatorDecrement, "--", new(lexer, 12..14)),
                new Token(TokenKind.SubAssignment, "-=", new(lexer, 15..17)),
                new Token(TokenKind.MulAssignment, "*=", new(lexer, 18..20)),
                new Token(TokenKind.DivAssignment, "/=", new(lexer, 21..23)),
                new Token(TokenKind.BooleanLEQ, "<=", new(lexer, 24..26)),
                new Token(TokenKind.BooleanGEQ, ">=", new(lexer, 27..29)),
                new Token(TokenKind.RangeDots, "..", new(lexer, 30..32)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 32..33))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestFullTokens_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", FULL_TOKENS);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.KeyReturn, "return", new(lexer, 0..6)),
                new Token(TokenKind.KeyContinue, "continue", new(lexer, 7..15)),
                new Token(TokenKind.KeyBreak, "break", new(lexer, 16..21)),
                new Token(TokenKind.KeyWhile, "while", new(lexer, 22..27)),
                new Token(TokenKind.KeyPub, "pub", new(lexer, 28..31)),
                new Token(TokenKind.KeyUse, "use", new(lexer, 32..35)),
                new Token(TokenKind.KeyImport, "import", new(lexer, 36..42)),
                new Token(TokenKind.KeyNew, "new", new(lexer, 43..46)),
                new Token(TokenKind.KeyFor, "for", new(lexer, 47..50)),
                new Token(TokenKind.KeyType, "type", new(lexer, 51..55)),
                new Token(TokenKind.KeyAs, "as", new(lexer, 56..58)),
                new Token(TokenKind.KeyIn, "in", new(lexer, 59..61)),
                new Token(TokenKind.KeyTo, "to", new(lexer, 62..64)),
                new Token(TokenKind.KeyIf, "if", new(lexer, 65..67)),
                new Token(TokenKind.KeyElif, "elif", new(lexer, 68..72)),
                new Token(TokenKind.KeyElse, "else", new(lexer, 73..77)),
                new Token(TokenKind.KeyFunc, "func", new(lexer, 78..82)),
                new Token(TokenKind.KeyVar, "var", new(lexer, 83..86)),
                new Token(TokenKind.KeyConst, "const", new(lexer, 87..92)),
                new Token(TokenKind.KeyTstr, "str", new(lexer, 93..96)),
                new Token(TokenKind.KeyTchr, "chr", new(lexer, 97..100)),
                new Token(TokenKind.KeyTi32, "i32", new(lexer, 107..110)),
                new Token(TokenKind.KeyTi64, "i64", new(lexer, 111..114)),
                new Token(TokenKind.KeyTu8, "u8", new(lexer, 115..117)),
                new Token(TokenKind.KeyTu32, "u32", new(lexer, 118..121)),
                new Token(TokenKind.KeyTu64, "u64", new(lexer, 122..125)),
                new Token(TokenKind.KeyTunknown, "unknown", new(lexer, 126..133)),
                new Token(TokenKind.KeyWhen, "when", new(lexer, 134..138)),
                new Token(TokenKind.KeyDeclare, "declare", new(lexer, 139..146)),
                new Token(TokenKind.KeyTVoid, "void", new(lexer, 147..151)),
                new Token(TokenKind.KeyTbool, "bool", new(lexer, 152..156)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 156..157))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestRandomTokens_CorrectTokenization()
        {
            Lexer lexer = new MugLexer("test", RANDOM_TOKENS);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.KeyReturn, "return", new(lexer, 0..6)),
                new Token(TokenKind.BooleanEQ, "==", new(lexer, 7..9)),
                new Token(TokenKind.OpenPar, "(", new(lexer, 10..11)),
                new Token(TokenKind.ClosePar, ")", new(lexer, 12..13)),
                new Token(TokenKind.AddAssignment, "+=", new(lexer, 14..16)),
                new Token(TokenKind.KeyContinue, "continue", new(lexer, 17..25)),
                new Token(TokenKind.KeyPub, "pub", new(lexer, 26..29)),
                new Token(TokenKind.Negation, "!", new(lexer, 30..31)),
                new Token(TokenKind.MulAssignment, "*=", new(lexer, 32..34)),
                new Token(TokenKind.RangeDots, "..", new(lexer, 35..37)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 37..38))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars01_OneChar()
        {
            Lexer lexer = new MugLexer("test", CHARS01);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.ConstantChar, "c", new(lexer, 0..3)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 3..4))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars02_OneChar()
        {
            Lexer lexer = new MugLexer("test", CHARS02);
            var ex = Assert.Throws<Zap.Compilation.CompilationException>(() => lexer.Tokenize());

            Assert.AreEqual("Char has not been correctly enclosed", ex.Diagnostic.GetErrors().First().Message);
        }

        [Test]
        public void TestChars03_TooManyChars()
        {
            Lexer lexer = new MugLexer("test", CHARS03);
            lexer.Tokenize();

            Assert.AreEqual("Too many characters in const char", lexer.DiagnosticBag.GetErrors().First().Message);
        }

        [Test]
        public void TestChars04_OneEscapedChar()
        {
            Lexer lexer = new MugLexer("test", CHARS04);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.ConstantChar, "\\", new(lexer, 0..4)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 4..5))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars05_TooManyEscapedChars()
        {
            Lexer lexer = new MugLexer("test", CHARS05);
            lexer.Tokenize();
            Assert.AreEqual(lexer.DiagnosticBag.Count, 1);

            Assert.AreEqual("Too many characters in const char", lexer.DiagnosticBag.GetErrors().First().Message);
        }

        [Test]
        public void TestBackticks01_OneSymbol()
        {
            Lexer lexer = new MugLexer("test", BACKTICKS01);
            lexer.Tokenize();

            List<Token> tokens = lexer.TokenCollection;

            List<Token> expected = new List<Token>
            {
                new Token(TokenKind.Identifier, "*", new(lexer, 0..3)),
                new Token(TokenKind.EOF, "<EOF>", new(lexer, 3..4))
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestBackticks02_OneChar()
        {
            Lexer lexer = new MugLexer("test", BACKTICKS02);
            lexer.Tokenize();

            Assert.AreEqual("Backtick sequence has not been correctly enclosed", lexer.DiagnosticBag.GetErrors().First().Message);
        }

        [Test]
        public void TestBackticks03_Empty()
        {
            Lexer lexer = new MugLexer("test", BACKTICKS03);
            lexer.Tokenize();

            Assert.AreEqual("Not enough characters in backtick sequence", lexer.DiagnosticBag.GetErrors().First().Message);
        }
    }
}
