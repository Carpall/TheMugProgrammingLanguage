using Mug.Compilation;
using Mug.Models.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Mug.Models.Lexer
{
    public struct Token : INode
    {
        public string NodeKind => "Literal";
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenKind Kind { get; }
        public string Value { get; }
        public ModulePosition Position { get; set; }

        public Token(TokenKind kind, string value, ModulePosition position)
        {
            Kind = kind;
            Value = value;
            Position = position;
        }

        public static Token NewInfo(TokenKind kind, string value)
        {
            return new Token(kind, value, new());
        }

        public override string ToString()
        {
            return $"{Position} {Kind}: {Value}";
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
