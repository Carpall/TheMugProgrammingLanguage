using Mug.Compilation;
using Mug.Models.Lexer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mug.Tests
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

        private Lexer newLexer(string test)
        {
            return new("test", test, new(null));
        }

        [Test]
        public void GetLength_EmptyCollection_ReturnZero()
        {
            var lexer = newLexer(OPERATION01);

            Assert.AreEqual(lexer.Length, 0);
        }

        [Test]
        public void GetLength_NonEmptyCollection_ReturnLength()
        {
            var lexer = newLexer(VARIABLE01);
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
            var lexer = newLexer(VARIABLE01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.KeyVar, "var", new(lexer, 0..3), false),
                new(TokenKind.Identifier, "x", new(lexer, 4..5), false),
                new(TokenKind.Equal, "=", new(lexer, 6..7), false),
                new(TokenKind.ConstantDigit, "0", new(lexer, 8..9), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 10..11), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test02_CorrectTokenization()
        {
            var lexer = newLexer(VARIABLE02);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.KeyVar, "var", new(lexer, 0..3), false),
                new(TokenKind.Identifier, "number", new(lexer, 4..10), false),
                new(TokenKind.Colon, ":", new(lexer, 10..11), false),
                new(TokenKind.Identifier, "i32", new(lexer, 12..15), false),
                new(TokenKind.Equal, "=", new(lexer, 16..17), false),
                new(TokenKind.ConstantDigit, "50", new(lexer, 18..20), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 21..22), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test03_CorrectTokenization()
        {
            var lexer = newLexer(VARIABLE03);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.ConstantDigit, "50", new(lexer, 1..3), false),
                new(TokenKind.Equal, "=", new(lexer, 4..5), false),
                new(TokenKind.Identifier, "i32", new(lexer, 6..9), false),
                new(TokenKind.Colon, ":", new(lexer, 10..11), false),
                new(TokenKind.Identifier, "number", new(lexer, 11..17), false),
                new(TokenKind.KeyVar, "var", new(lexer, 18..21), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 21..22), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test04_CorrectTokenization()
        {
            var lexer = newLexer(VARIABLE04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.Identifier, "varnumber", new(lexer, 0..9), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 9..10), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void Test05_CorrectTokenization()
        {
            var lexer = newLexer(VARIABLE05);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.Identifier, "i33", new(lexer, 0..3), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 3..4), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments01_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = newLexer(COMMENTS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.EOF, "<EOF>", new(lexer, 20..21), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments02_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = newLexer(COMMENTS02);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.EOF, "<EOF>", new(lexer, 36..37), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments03_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = newLexer(COMMENTS03);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.EOF, "<EOF>", new(lexer, 43..44), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments04_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = newLexer(COMMENTS04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.Identifier, "multi", new(lexer, 23..28), false),
                new(TokenKind.Minus, "-", new(lexer, 28..29), false),
                new(TokenKind.Identifier, "line", new(lexer, 29..33), false),
                new(TokenKind.Identifier, "comment", new(lexer, 34..41), false),
                new(TokenKind.Star, "*", new(lexer, 42..43), false),
                new(TokenKind.Slash, "/", new(lexer, 43..44), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 44..45), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestComments05_CorrectTokenization()
        {
            // A comments gets consumed, turning it into an empty string
            var lexer = newLexer(COMMENTS05);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.Identifier, "multi", new(lexer, 26..31), false),
                new(TokenKind.Minus, "-", new(lexer, 31..32), false),
                new(TokenKind.Identifier, "line", new(lexer, 32..36), false),
                new(TokenKind.Identifier, "comment", new(lexer, 37..44), false),
                new(TokenKind.Star, "*", new(lexer, 45..46), false),
                new(TokenKind.Slash, "/", new(lexer, 46..47), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 47..48), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void EmptyString_CorrectTokenization()
        {
            // An empty string gets converted into an <EOF>
            var lexer = newLexer(EMPTYSTRING);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.EOF, "<EOF>", new(lexer, 0..1), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings01_CorrectTokenization()
        {
            var lexer = newLexer(STRINGS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.ConstantString, "This is a string", new(lexer, 0..18), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 18..19), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings02_ExceptionCaught()
        {
            var lexer = newLexer(STRINGS02);
            lexer.Tokenize();

            Assert.AreEqual("Constant string has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestStrings03_CorrectTokenization()
        {
            var lexer = newLexer(STRINGS03);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.ConstantString, "This is a ", new(lexer, 0..12), false),
                new(TokenKind.Identifier, "nested", new(lexer, 13..19), false),
                new(TokenKind.ConstantString, "string", new(lexer, 20..28), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 28..29), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings04_EscapedChars()
        {
            var lexer = newLexer(STRINGS04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.ConstantString, "\n\t\r\"", new(lexer, 0..10), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 10..11), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings05_AdvancedEscapedChars()
        {
            var lexer = newLexer(STRINGS05);
            lexer.Tokenize();
            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.Identifier, "i32", new(lexer, 0 ..3), false),
                new(TokenKind.ConstantString, "\\ ", new(lexer, 3 ..8), false),
                new(TokenKind.Identifier, "t", new(lexer, 8 ..9), false),
                new(TokenKind.Identifier, "token", new(lexer, 10..15), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 15 ..16), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestStrings06_ExceptionCaught()
        {
            var lexer = newLexer(STRINGS06);
            lexer.Tokenize();

            Assert.AreEqual("String has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestSingleTokens_CorrectTokenization()
        {
            var lexer = newLexer(SINGLE_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.OpenPar, "(", new(lexer, 0..1), false),
                new(TokenKind.ClosePar, ")", new(lexer, 2..3), false),
                new(TokenKind.OpenBracket, "[", new(lexer, 4..5), false),
                new(TokenKind.CloseBracket, "]", new(lexer, 6..7), false),
                new(TokenKind.OpenBrace, "{", new(lexer, 8..9), false),
                new(TokenKind.CloseBrace, "}", new(lexer, 10..11), false),
                new(TokenKind.BooleanLess, "<", new(lexer, 12..13), false),
                new(TokenKind.BooleanGreater, ">", new(lexer, 14..15), false),
                new(TokenKind.Equal, "=", new(lexer, 16..17), false),
                new(TokenKind.Negation, "!", new(lexer, 18..19), false),
                new(TokenKind.BooleanAND, "&", new(lexer, 20..21), false),
                new(TokenKind.BooleanOR, "|", new(lexer, 22..23), false),
                new(TokenKind.Plus, "+", new(lexer, 24..25), false),
                new(TokenKind.Minus, "-", new(lexer, 26..27), false),
                new(TokenKind.Star, "*", new(lexer, 28..29), false),
                new(TokenKind.Slash, "/", new(lexer, 30..31), false),
                new(TokenKind.Comma, ",", new(lexer, 32..33), false),
                new(TokenKind.Colon, ":", new(lexer, 36..37), false),
                new(TokenKind.Dot, ".", new(lexer, 38..39), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 41..42), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestDoubleTokens_CorrectTokenization()
        {
            var lexer = newLexer(DOUBLE_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.BooleanEQ, "==", new(lexer, 0..2), false),
                new(TokenKind.BooleanNEQ, "!=", new(lexer, 3..5), false),
                new(TokenKind.OperatorIncrement, "++", new(lexer, 6..8), false),
                new(TokenKind.AddAssignment, "+=", new(lexer, 9..11), false),
                new(TokenKind.OperatorDecrement, "--", new(lexer, 12..14), false),
                new(TokenKind.SubAssignment, "-=", new(lexer, 15..17), false),
                new(TokenKind.MulAssignment, "*=", new(lexer, 18..20), false),
                new(TokenKind.DivAssignment, "/=", new(lexer, 21..23), false),
                new(TokenKind.BooleanLEQ, "<=", new(lexer, 24..26), false),
                new(TokenKind.BooleanGEQ, ">=", new(lexer, 27..29), false),
                new(TokenKind.RangeDots, "..", new(lexer, 30..32), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 32..33), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestFullTokens_CorrectTokenization()
        {
            var lexer = newLexer(FULL_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.KeyReturn, "return", new(lexer, 0..6), false),
                new(TokenKind.KeyContinue, "continue", new(lexer, 7..15), false),
                new(TokenKind.KeyBreak, "break", new(lexer, 16..21), false),
                new(TokenKind.KeyWhile, "while", new(lexer, 22..27), false),
                new(TokenKind.KeyPub, "pub", new(lexer, 28..31), false),
                new(TokenKind.KeyUse, "use", new(lexer, 32..35), false),
                new(TokenKind.KeyImport, "import", new(lexer, 36..42), false),
                new(TokenKind.KeyNew, "new", new(lexer, 43..46), false),
                new(TokenKind.KeyFor, "for", new(lexer, 47..50), false),
                new(TokenKind.KeyType, "type", new(lexer, 51..55), false),
                new(TokenKind.KeyAs, "as", new(lexer, 56..58), false),
                new(TokenKind.KeyIn, "in", new(lexer, 59..61), false),
                new(TokenKind.KeyTo, "to", new(lexer, 62..64), false),
                new(TokenKind.KeyIf, "if", new(lexer, 65..67), false),
                new(TokenKind.KeyElif, "elif", new(lexer, 68..72), false),
                new(TokenKind.KeyElse, "else", new(lexer, 73..77), false),
                new(TokenKind.KeyFunc, "func", new(lexer, 78..82), false),
                new(TokenKind.KeyVar, "var", new(lexer, 83..86), false),
                new(TokenKind.KeyConst, "const", new(lexer, 87..92), false),
                new(TokenKind.Identifier, "str", new(lexer, 93..96), false),
                new(TokenKind.Identifier, "chr", new(lexer, 97..100), false),
                new(TokenKind.Identifier, "i32", new(lexer, 107..110), false),
                new(TokenKind.Identifier, "i64", new(lexer, 111..114), false),
                new(TokenKind.Identifier, "u8", new(lexer, 115..117), false),
                new(TokenKind.Identifier, "u32", new(lexer, 118..121), false),
                new(TokenKind.Identifier, "u64", new(lexer, 122..125), false),
                new(TokenKind.Identifier, "unknown", new(lexer, 126..133), false),
                new(TokenKind.Identifier, "when", new(lexer, 134..138), false),
                new(TokenKind.Identifier, "declare", new(lexer, 139..146), false),
                new(TokenKind.Identifier, "void", new(lexer, 147..151), false),
                new(TokenKind.Identifier, "bool", new(lexer, 152..156), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 156..157), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestRandomTokens_CorrectTokenization()
        {
            var lexer = newLexer(RANDOM_TOKENS);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.KeyReturn, "return", new(lexer, 0..6), false),
                new(TokenKind.BooleanEQ, "==", new(lexer, 7..9), false),
                new(TokenKind.OpenPar, "(", new(lexer, 10..11), false),
                new(TokenKind.ClosePar, ")", new(lexer, 12..13), false),
                new(TokenKind.AddAssignment, "+=", new(lexer, 14..16), false),
                new(TokenKind.KeyContinue, "continue", new(lexer, 17..25), false),
                new(TokenKind.KeyPub, "pub", new(lexer, 26..29), false),
                new(TokenKind.Negation, "!", new(lexer, 30..31), false),
                new(TokenKind.MulAssignment, "*=", new(lexer, 32..34), false),
                new(TokenKind.RangeDots, "..", new(lexer, 35..37), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 37..38), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars01_OneChar()
        {
            var lexer = newLexer(CHARS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.ConstantChar, "c", new(lexer, 0..3), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 3..4), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars02_OneChar()
        {
            var lexer = newLexer(CHARS02);
            lexer.Tokenize();

            Assert.AreEqual("Constant char has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestChars03_TooManyChars()
        {
            var lexer = newLexer(CHARS03);
            lexer.Tokenize();

            Assert.AreEqual("Too many characters in const char", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestChars04_OneEscapedChar()
        {
            var lexer = newLexer(CHARS04);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.ConstantChar, "\\", new(lexer, 0..4), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 4..5), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestChars05_TooManyEscapedChars()
        {
            var lexer = newLexer(CHARS05);
            lexer.Tokenize();

            Assert.AreEqual("Too many characters in const char", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestBackticks01_OneSymbol()
        {
            var lexer = newLexer(BACKTICKS01);
            lexer.Tokenize();

            var tokens = lexer.TokenCollection;

            var expected = new List<Token>
            {
                new(TokenKind.Identifier, "*", new(lexer, 0..3), false),
                new(TokenKind.EOF, "<EOF>", new(lexer, 3..4), false)
            };

            AreListEqual(expected, tokens);
        }

        [Test]
        public void TestBackticks02_OneChar()
        {
            var lexer = newLexer(BACKTICKS02);
            lexer.Tokenize();

            Assert.AreEqual("Backtick sequence has not been correctly enclosed", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }

        [Test]
        public void TestBackticks03_Empty()
        {
            var lexer = newLexer(BACKTICKS03);
            lexer.Tokenize();

            Assert.AreEqual("Not enough characters in backtick sequence", lexer.Tower.Diagnostic.GetAlerts().First().Message);
        }
    }
}
