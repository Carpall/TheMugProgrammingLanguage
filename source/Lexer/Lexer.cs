using Mug.Compilation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mug.Tokenizer
{
    public class Lexer : CompilerComponent
    {
        public List<Token> TokenCollection { get; set; }

        public readonly string Source;
        public readonly string ModuleName;
        public readonly char[] ValidBacktickSequenceCharacters = { '[', ']', '!', '-', '+', '*', '/', '=', '$', '^', '~' };

        private StringBuilder CurrentSymbol { get; set; }
        private bool _eol = false;
        private int CurrentIndex { get; set; }

        /// <summary>
        /// restores all the fields to their default values
        /// </summary>
        public void Reset()
        {
            TokenCollection = new();
            CurrentIndex = 0;
            CurrentSymbol = new();
        }

        public int Length
        {
            get
            {
                return TokenCollection == null ? 0 : TokenCollection.Count;
            }
        }

        private char Current
        {
            get
            {
                return DoesNotMatchEOF() ? Source[CurrentIndex] : '\0';
            }
        }

        public string ModuleRelativePath => Path.GetRelativePath(Environment.CurrentDirectory, ModuleName);

        public Lexer(string moduleName, string source, CompilationTower tower) : base(tower)
        {
            ModuleName = moduleName;
            Source = source;
        }

        private ModulePosition ModPos(Range position)
        {
            return new(this, position);
        }

        private bool GetEOL()
        {
            var eol = _eol;
            _eol = false;
            return eol;
        }

        /// <summary>
        /// adds a keyword to the tokens stream and returns true
        /// </summary>
        private bool AddKeyword(TokenKind kind, string keyword)
        {
            TokenCollection.Add(new(kind, keyword, ModPos((CurrentIndex - keyword.Length)..CurrentIndex), GetEOL()));
            return true;
        }

        /// <summary>
        /// returns true and insert a keyword token if s is a keyword, otherwise returns false, see the caller to understand better
        /// </summary>
        private static bool CheckAndSetKeyword(string s, out TokenKind kind)
        {
            /*
             * could be converted via reflection but it costs to much
             * could be converted via array sorting but it doesn't make any difference,
               at least could cost more (Array.IndexOf compares all elements with the passed one, but has the overhead of the cycle)
            */
            kind = s switch
            {
                "return" => TokenKind.KeyReturn,
                "continue" => TokenKind.KeyContinue,
                "break" => TokenKind.KeyBreak,
                "while" => TokenKind.KeyWhile,
                "pub" => TokenKind.KeyPub,
                "priv" => TokenKind.KeyPriv,
                "use" => TokenKind.KeyUse,
                "import" => TokenKind.KeyImport,
                "new" => TokenKind.KeyNew,
                "for" => TokenKind.KeyFor,
                "type" => TokenKind.KeyType,
                "enum" => TokenKind.KeyEnum,
                "as" => TokenKind.KeyAs,
                "is" => TokenKind.KeyIs,
                "in" => TokenKind.KeyIn,
                "and" => TokenKind.BooleanAND,
                "or" => TokenKind.BooleanOR,
                "if" => TokenKind.KeyIf,
                "elif" => TokenKind.KeyElif,
                "else" => TokenKind.KeyElse,
                "fn" => TokenKind.KeyFunc,
                "var" => TokenKind.KeyVar,
                "const" => TokenKind.KeyConst,
                "catch" => TokenKind.KeyCatch,
                "switch" => TokenKind.KeySwitch,
                "try" => TokenKind.KeyTry,
                _ => TokenKind.Bad
            };

            return kind != TokenKind.Bad;
        }

        private T InExpressionError<T>(string error)
        {
            Tower.Report(this, CurrentIndex, error);
            return default;
        }

        /// <summary>
        /// checks if there is a next char, to avoid index out of range exception
        /// </summary>
        private bool HasNext()
        {
            return CurrentIndex + 1 < Source.Length;
        }

        private char GetNext()
        {
            return Source[CurrentIndex + 1];
        }

        /// <summary>
        /// recognizes a single symbol or launches compilation-error
        /// </summary>
        private static TokenKind GetSingle(char c) => (TokenKind)c;

        private void AddToken(TokenKind kind, string value)
        {
            if (value is not null)
                TokenCollection.Add(new(kind, value, ModPos((CurrentIndex - value.ToString().Length)..CurrentIndex), GetEOL()));
            else // chatching null reference exception
                TokenCollection.Add(new(kind, value, ModPos(CurrentIndex..(CurrentIndex + 1)), GetEOL()));
        }

        /// <summary>
        /// adds a single symbol
        /// </summary>
        private void AddSingle(TokenKind kind, string value)
        {
            TokenCollection.Add(new(kind, value, ModPos(CurrentIndex..(CurrentIndex + 1)), GetEOL()));
        }

        /// <summary>
        /// adds a double symbol
        /// </summary>
        private void AddDouble(TokenKind kind, string value)
        {
            /*
             * current index as start position
             * moves the index by one: a double token occupies 2 chars
             */

            TokenCollection.Add(new(kind, value, ModPos(CurrentIndex..(++CurrentIndex + 1)), GetEOL()));
        }

        /// <summary>
        /// tests if value is a boolean constant
        /// </summary>
        private static bool IsBoolean(string value)
        {
            return value == "true" || value == "false";
        }

        /// <summary>
        /// matches '#'
        /// </summary>
        private bool MatchInlineComment()
        {
            return HasNext() && Source[CurrentIndex] == '/' && GetNext() == '/';
        }

        /// <summary>
        /// match if the line ends or the source ends
        /// </summary>
        private bool MatchEolOrEof()
        {
            return CurrentIndex == Source.Length || Source[CurrentIndex] == '\n';
        }

        /// <summary>
        /// checks if there is '#['
        /// </summary>
        private bool MatchStartMultiLineComment()
        {
            return HasNext() && Source[CurrentIndex] == '/' && GetNext() == '*';
        }

        /// <summary>
        /// checks if there is ']#'
        /// </summary>
        private bool MatchEndMultiLineComment()
        {
            return HasNext() && Source[CurrentIndex] == '*' && GetNext() == '/';
        }

        /// <summary>
        /// eats comments
        /// </summary>
        private void ConsumeComments()
        {
            if (MatchStartMultiLineComment())
            {
                EatFirstTwoCommentChars();
                EatMultiLineComment();
                EatLastTwoCommentChars();
            }
            else if (MatchInlineComment())
                EatInlineComment();
        }

        private void EatInlineComment()
        {
            while (!MatchEolOrEof())
                CurrentIndex++;
        }

        private void EatLastTwoCommentChars()
        {
            if (MatchEndMultiLineComment())
                CurrentIndex += 2;
        }

        private void EatMultiLineComment()
        {
            while (!MatchEndMultiLineComment() && CurrentIndex != Source.Length)
                CurrentIndex++;
        }

        private void EatFirstTwoCommentChars()
        {
            CurrentIndex += 2;
        }

        /// <summary>
        /// collects a constant character
        /// </summary>
        private void CollectChar()
        {
            var start = CurrentIndex++;

            //consume string until EOF or closed " is found
            while (DoesNotMatchEOFOrClose('\''))
            {
                var c = Source[CurrentIndex++];
                CollectEscapedCharIfNeeded(ref c);

                CurrentSymbol.Append(c);
            }

            var end = CurrentIndex;

            ReportNotCorrectlyEnclosedIfNeeded('\'', "Constant char");

            end++;

            ReportCharLengthIfNeeded(start, end);
            AddCurrentSymbol(TokenKind.ConstantChar, start, end);
        }

        private void AddCurrentSymbol(TokenKind kind, int start, int end)
        {
            TokenCollection.Add(new(kind, CurrentSymbol.ToString(), ModPos(start..end), GetEOL()));
            CurrentSymbol.Clear();
        }

        private void ReportCharLengthIfNeeded(int start, int end)
        {
            if (CurrentSymbol.Length > 1)
                Tower.Report(ModPos(start..end), "Too many characters in const char");
            else if (CurrentSymbol.Length < 1)
                Tower.Report(ModPos(start..end), "Not enough characters in const char");
        }

        private void ReportNotCorrectlyEnclosedIfNeeded(char delimiter, string kind)
        {
            if (CurrentIndex == Source.Length && Source[CurrentIndex - 1] != delimiter)
                Tower.Report(this, CurrentIndex - 1, $"{kind} has not been correctly enclosed");
        }

        private bool DoesNotMatchEOFOrClose(char delimiter)
        {
            return DoesNotMatchEOF() && Source[CurrentIndex] != delimiter;
        }

        private void CollectEscapedCharIfNeeded(ref char c)
        {
            if (c == '\\')
            {
                c = RecognizeEscapedChar(Current);
                CurrentIndex++;
            }
        }

        private char RecognizeEscapedChar(char escapedchar)
        {
            return escapedchar switch
            {
                'n' => '\n',
                't' => '\t',
                'r' => '\r',
                '0' => '\0',
                '\'' or '"' or '\\' => escapedchar,
                _ => InExpressionError<char>("Unable to recognize escaped char")
            };
        }

        /// <summary>
        /// collects a string, now it does not support the escaped chars yet
        /// </summary>
        private void CollectString()
        {
            var start = CurrentIndex++;

            //consume string until EOF or closed " is found
            while (DoesNotMatchEOFOrClose('"'))
            {
                var c = Source[CurrentIndex++];
                CollectEscapedCharIfNeeded(ref c);

                CurrentSymbol.Append(c);
            }

            var end = CurrentIndex;

            ReportNotCorrectlyEnclosedIfNeeded('"', "Constant string");

            AddCurrentSymbol(TokenKind.ConstantString, start, end + 1);
        }

        private bool IsValidBackTickSequence(string sequence)
        {
            for (var i = 0; i < sequence.Length; i++)
            {
                var chr = sequence[i];

                if (!ValidBacktickSequenceCharacters.Contains(chr))
                    return false;
            }

            return true;
        }

        private static bool IsKeyword(string sequence)
        {
            return CheckAndSetKeyword(sequence, out _);
        }

        /// <summary>
        /// collects a symbol incapsulated in a backtick string and add it to the token stream as identifier
        /// </summary>
        private void CollectBacktick()
        {
            var start = CurrentIndex++;

            while (DoesNotMatchEOFOrClose('`'))
                CurrentSymbol.Append(Source[CurrentIndex++]);

            var end = CurrentIndex;

            ReportNotCorrectlyEnclosedIfNeeded('`', "Backtick sequence");

            end++;

            var sequence = CurrentSymbol.ToString();
            ReportBadBacktickSequenceIfNeeded(sequence, ModPos(start..end));

            //else add closing simbol, removing whitespaces
            AddCurrentSymbol(TokenKind.Identifier, start, end);
        }

        private void ReportBadBacktickSequenceIfNeeded(string sequence, ModulePosition position)
        {
            if (sequence.Length < 1)
                Tower.Report(position, "Not enough characters in backtick sequence");
            if (!IsValidBackTickSequence(sequence) && !IsKeyword(sequence))
                Tower.Report(position, "Invalid backtick sequence");
        }

        /// <summary>
        /// follows identifier rules
        /// </summary>
        private static bool IsValidIdentifierChar(char current)
        {
            return char.IsLetter(current) || current == '_';
        }

        /// <summary>
        /// tests if current is an escaped char or a white space
        /// </summary>
        private static bool IsSkippableControl(char current)
        {
            return (char.IsControl(current) || char.IsWhiteSpace(current));
        }

        /// <summary>
        /// checks if there is a double symbol else add a single symbol
        /// </summary>
        private void ProcessSpecial(char current)
        {
            switch (current)
            {
                case '"': CollectString(); break;
                case '\'':  CollectChar(); break;
                case '`': CollectBacktick(); break;
                default: ProcessSymbol(current); break;
            }
        }

        private void ProcessSymbol(char current)
        {
            if (!HasNext())
            {
                AddSingle(GetSingle(current), current.ToString());
                return;
            }

            ProcessDoubleTokenOrSingle(current);
        }

        private void ProcessDoubleTokenOrSingle(char current)
        {
            var doubleToken = current.ToString() + GetNext();

            // checks if there is a double token
            switch (doubleToken)
            {
                case "==": add(TokenKind.BooleanEQ); break;
                case "!=": add(TokenKind.BooleanNEQ); break;
                case "++": add(TokenKind.OperatorIncrement); break;
                case "+=": add(TokenKind.AddAssignment); break;
                case "--": add(TokenKind.OperatorDecrement); break;
                case "-=": add(TokenKind.SubAssignment); break;
                case "*=": add(TokenKind.MulAssignment); break;
                case "/=": add(TokenKind.DivAssignment); break;
                case "<=": add(TokenKind.BooleanLEQ); break;
                case ">=": add(TokenKind.BooleanGEQ); break;
                case "..": add(TokenKind.RangeDots); break;
                default: AddSingle(GetSingle(current), current.ToString()); break;
            }

            void add(TokenKind kind) => AddDouble(kind, doubleToken);
        }

        private void CollectIdentifer()
        {
            CollectHomogeneousWord();

            var value = CurrentSymbol.ToString();
            CurrentSymbol.Clear();

            AddIdentifierOrKeywordOrConstantBoolean(value);
        }

        private void AddIdentifierOrKeywordOrConstantBoolean(string value)
        {
            if (CheckAndSetKeyword(value, out var kind))
                AddKeyword(kind, value);
            else if (IsBoolean(value))
                AddToken(TokenKind.ConstantBoolean, value);
            else
                AddToken(TokenKind.Identifier, value);

            CurrentIndex--;
        }

        private void CollectHomogeneousWord()
        {
            do
                CurrentSymbol.Append(Source[CurrentIndex++]);
            while (HasCurrentAndItIsValidIdentifierChar());
        }

        private bool HasCurrentAndItIsValidIdentifierChar()
        {
            return DoesNotMatchEOF() && (IsValidIdentifierChar(Current) || char.IsDigit(Current));
        }

        private void CollectDigit()
        {
            var pos = CurrentIndex;
            var isfloat = false;
            var skipConstantFloatF = false;

            CollectConstantDigit(ref isfloat, ref skipConstantFloatF);

            if (!skipConstantFloatF)
                CheckConstantFloatF(ref isfloat);

            ReportOverflowConstantIfNeeded(ModPos(pos..CurrentIndex));

            AddCurrentSymbol(isfloat ? TokenKind.ConstantFloatDigit : TokenKind.ConstantDigit, pos, CurrentIndex);

            CurrentIndex--;
        }

        private void CollectConstantDigit(ref bool isfloat, ref bool skipConstantFloatF)
        {
            do
            {
                if (Current == '.' &&
                    CollectFloatDecimalPartAndReturnTrueIfHasToBreak(ref isfloat, ref skipConstantFloatF))
                    break;
                else if (Current != '_')
                    CurrentSymbol.Append(Current);

                CurrentIndex++;
            }
            while (DoesNotMatchEOFAndCurrentIsFloatValidConstantChar());
        }

        private bool DoesNotMatchEOFAndCurrentIsFloatValidConstantChar()
        {
            return DoesNotMatchEOF() && (char.IsDigit(Current) || Current == '.' || Current == '_');
        }

        private bool DoesNotMatchEOF()
        {
            return CurrentIndex < Source.Length;
        }

        private bool CollectFloatDecimalPartAndReturnTrueIfHasToBreak(ref bool isfloat, ref bool skipConstantFloatF)
        {
            if (CurrentIndex >= Source.Length || !char.IsDigit(Source[CurrentIndex + 1]))
                return skipConstantFloatF = true;

            if (isfloat)
                Tower.Report(this, CurrentIndex, "Invalid dot here");

            isfloat = true;
            CurrentSymbol.Append(',');

            return false;
        }

        private void CheckConstantFloatF(ref bool isfloat)
        {
            if (Current == 'f')
            {
                CurrentIndex++;
                isfloat = true;
            }
        }

        private void ReportOverflowConstantIfNeeded(ModulePosition position)
        {
            if (CurrentSymbol.Length >= 21)
                Tower.Report(position, "Constant overflow");
        }

        private bool MatchEOL()
        {
            var oldIndex = CurrentIndex;
            while (HasNext() && Current == '\n')
                CurrentIndex += 1;

            return IndexIsAdvanced(oldIndex);
        }

        private bool IndexIsAdvanced(int oldIndex)
        {
            return CurrentIndex != oldIndex;
        }

        /// <summary>
        /// recognize the kind of the char
        /// </summary>
        private void ProcessCurrentChar()
        {
            SetUpEOL();

            ConsumeComments();

            if (ReachedEOF())
                return;

            SetUpEOL();

            ProcessChar(Current);
        }

        private void ProcessChar(char current)
        {
            if (IsSkippableControl(current))
                return;

            if (IsValidIdentifierChar(current)) // identifiers
                CollectIdentifer();
            else if (char.IsDigit(current)) // digits
                CollectDigit();
            else
                ProcessSpecial(current); // if current is not a valid id char, a control or a string quote
        }

        private void SetUpEOL()
        {
            if (!_eol)
                _eol = MatchEOL();
        }

        private bool ReachedEOF()
        {
            return CurrentIndex >= Source.Length;
        }

        /// <summary>
        /// generates a token stream from a string
        /// </summary>
        public List<Token> Tokenize()
        {
            // set up all fields
            Reset();

            // go to the next char while there is one
            while (DoesNotMatchEOF())
            {
                ProcessCurrentChar();
                CurrentIndex++;
            }

            // end of file token
            AddSingle(TokenKind.EOF, "<EOF>");

            return TokenCollection;
        }
    }
}
