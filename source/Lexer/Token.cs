using Mug.Compilation;
using Mug.Parser;
using Mug.Parser.AST;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Tokenizer
{
    public struct Token : INode
    {
        public string NodeName => "Literal";
        public TokenKind Kind { get; }
        public string Value { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsOnNewLine { get; }

        public Token(TokenKind kind, string value, ModulePosition position, bool isonnewline)
        {
            Kind = kind;
            Value = value;
            Position = position;
            IsOnNewLine = isonnewline;
        }

        public static Token NewInfo(TokenKind kind, string value)
        {
            return new Token(kind, value, default, default);
        }

        public override string ToString()
        {
            return $"{Kind}('{Value}'), OnNewLine: {IsOnNewLine}";
        }

        /// <summary>
        /// tests
        /// </summary>
        public override bool Equals(object other)
        {
            return other is Token token &&
                   token.Kind.Equals(this.Kind) &&
                   token.Value.Equals(this.Value) &&
                   token.Position.Equals(this.Position);
        }
    }
}
