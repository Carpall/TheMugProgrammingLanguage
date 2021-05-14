using Nylon.Compilation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nylon.Models.Lexer
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
                return Source[CurrentIndex];
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
                "to" => TokenKind.KeyTo,
                "if" => TokenKind.KeyIf,
                "elif" => TokenKind.KeyElif,
                "else" => TokenKind.KeyElse,
                "func" => TokenKind.KeyFunc,
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
                // eats first two chars '//'
                CurrentIndex += 2;

                while (!MatchEndMultiLineComment() && CurrentIndex != Source.Length)
                    CurrentIndex++;

                if (MatchEndMultiLineComment())
                    CurrentIndex += 2;
            }
            else if (MatchInlineComment())
                while (!MatchEolOrEof())
                    CurrentIndex++;
        }

        /// <summary>
        /// collects a constant character
        /// </summary>
        private void CollectChar()
        {
            var start = CurrentIndex++;

            //consume string until EOF or closed " is found
            while (CurrentIndex < Source.Length && Source[CurrentIndex] != '\'')
            {
                char c = Source[CurrentIndex++];

                if (c == '\\')
                {
                    c = RecognizeEscapedChar(Current); // @1
                    CurrentIndex++; // cursor now points to the next character after the escaped one, if operated at @1 the position was broken with unrecognized chars
                }

                CurrentSymbol.Append(c);
            }

            var end = CurrentIndex;

            //if you found an EOF, throw
            if (CurrentIndex == Source.Length && Source[CurrentIndex - 1] != '"')
                Tower.Report(this, CurrentIndex - 1, $"Char has not been correctly enclosed");

            end++;

            //longer than one char
            if (CurrentSymbol.Length > 1)
                Tower.Report(ModPos(start..end), "Too many characters in const char");
            else if (CurrentSymbol.Length < 1)
                Tower.Report(ModPos(start..end), "Not enough characters in const char");

            //else add closing simbol
            TokenCollection.Add(new(TokenKind.ConstantChar, CurrentSymbol.ToString(), ModPos(start..end), GetEOL()));
            CurrentSymbol.Clear();
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

            if (CurrentIndex == Source.Length)
                reportNotCorrectlyEnclosed();

            //consume string until EOF or closed " is found
            while (CurrentIndex < Source.Length && Source[CurrentIndex] != '"')
            {
                char c = Source[CurrentIndex++];

                if (c == '\\')
                    c = RecognizeEscapedChar(Source[CurrentIndex++]);

                CurrentSymbol.Append(c);
            }

            var end = CurrentIndex;

            //if you found an EOF, throw
            if (CurrentIndex == Source.Length && Source[CurrentIndex - 1] != '"')
                reportNotCorrectlyEnclosed();

            //else add closing simbol
            TokenCollection.Add(new(TokenKind.ConstantString, CurrentSymbol.ToString(), ModPos(start..(end + 1)), GetEOL()));
            CurrentSymbol.Clear();

            void reportNotCorrectlyEnclosed() {
                Tower.Report(this, start, $"String has not been correctly enclosed");
            }
        }

        private bool IsValidBackTickSequence(string sequence)
        {
            for (int i = 0; i < sequence.Length; i++)
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

            //consume string until EOF or closed ` is found
            while (CurrentIndex < Source.Length && Source[CurrentIndex] != '`')
                CurrentSymbol.Append(Source[CurrentIndex++]);

            var end = CurrentIndex;

            //if you found an EOF, throw
            if (CurrentIndex == Source.Length && Source[CurrentIndex - 1] != '`')
                Tower.Report(this, CurrentIndex - 1, $"Backtick sequence has not been correctly enclosed");

            var pos = ModPos(start..(end + 1));

            if (CurrentSymbol.Length < 1)
                Tower.Report(pos, "Not enough characters in backtick sequence");

            string sequence = CurrentSymbol.ToString();

            if (!IsValidBackTickSequence(sequence) && !IsKeyword(sequence))
                Tower.Report(pos, "Invalid backtick sequence");

            //else add closing simbol, removing whitespaces
            TokenCollection.Add(new(TokenKind.Identifier, sequence, pos, GetEOL()));
            CurrentSymbol.Clear();
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
            if (current == '"') CollectString();
            else if (current == '\'') CollectChar();
            else if (current == '`') CollectBacktick();
            else
            {
                if (!HasNext())
                {
                    AddSingle(GetSingle(current), current.ToString());
                    return;
                }

                var doubleToken = current.ToString() + GetNext();

                // checks if there is a double token
                switch (doubleToken)
                {
                    case "==": AddDouble(TokenKind.BooleanEQ, doubleToken); break;
                    case "!=": AddDouble(TokenKind.BooleanNEQ, doubleToken); break;
                    case "++": AddDouble(TokenKind.OperatorIncrement, doubleToken); break;
                    case "+=": AddDouble(TokenKind.AddAssignment, doubleToken); break;
                    case "--": AddDouble(TokenKind.OperatorDecrement, doubleToken); break;
                    case "-=": AddDouble(TokenKind.SubAssignment, doubleToken); break;
                    case "*=": AddDouble(TokenKind.MulAssignment, doubleToken); break;
                    case "/=": AddDouble(TokenKind.DivAssignment, doubleToken); break;
                    case "<=": AddDouble(TokenKind.BooleanLEQ, doubleToken); break;
                    case ">=": AddDouble(TokenKind.BooleanGEQ, doubleToken); break;
                    case "..": AddDouble(TokenKind.RangeDots, doubleToken); break;
                    default: AddSingle(GetSingle(current), current.ToString()); break;
                }
            }
        }

        private void CollectIdentifer()
        {
            do
                CurrentSymbol.Append(Source[CurrentIndex++]);
            while (CurrentIndex < Source.Length && (IsValidIdentifierChar(Current) || char.IsDigit(Current)));
            
            var value = CurrentSymbol.ToString();
            CurrentSymbol.Clear();

            if (CheckAndSetKeyword(value, out var kind))
                AddKeyword(kind, value);
            else
            {
                if (IsBoolean(value))
                    AddToken(TokenKind.ConstantBoolean, value);
                else
                    AddToken(TokenKind.Identifier, value);
            }
            
            CurrentIndex--;
        }

        private void CollectDigit()
        {
            var pos = CurrentIndex;
            var isfloat = false;
            do
            {
                if (Current == '.')
                {
                    if (CurrentIndex >= Source.Length || !char.IsDigit(Source[CurrentIndex + 1]))
                        goto end;

                    if (isfloat)
                        Tower.Report(this, CurrentIndex, "Invalid dot here");

                    isfloat = true;
                    CurrentSymbol.Append(',');
                }
                else if (Current != '_')
                    CurrentSymbol.Append(Current);

                CurrentIndex++;
            }
            while (CurrentIndex < Source.Length && (char.IsDigit(Current) || Current == '.' || Current == '_'));

            if (Current == 'f')
            {
                CurrentIndex++;
                isfloat = true;
            }

        end:

            var position = ModPos(pos..CurrentIndex);
            var s = CurrentSymbol.ToString();

            // could overflow
            if (s.Length >= 21)
                Tower.Report(position, "Constant overflow");

            TokenCollection.Add(new(isfloat ? TokenKind.ConstantFloatDigit : TokenKind.ConstantDigit, s, position, GetEOL()));
            CurrentSymbol.Clear();

            CurrentIndex--;
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
            // remove useless comments
            ConsumeComments();

            if (ReachedEOF())
                return;

            if (!_eol)
                _eol = MatchEOL();

            var current = Current;

            if (IsSkippableControl(current))
                return;

            if (IsValidIdentifierChar(current)) // identifiers
                CollectIdentifer();
            else if (char.IsDigit(current)) // digits
                CollectDigit();
            else
                ProcessSpecial(current); // if current is not a valid id char, a control or a string quote
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
            while (CurrentIndex < Source.Length)
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
