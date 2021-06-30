using Mug.Compilation;
using Mug.Syntax;
using Mug.Syntax.AST;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.Grammar
{
    public struct Token : INode
    {
        public string NodeName => "Literal";
        public TokenKind Kind { get; }
        public string Value { get; set; }
        public ModulePosition Position { get; set; }
        [JsonIgnore]
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

        public static Token NewInfo(TokenKind kind, string value, ModulePosition position)
        {
            return new Token(kind, value, position, default);
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

        public override string ToString()
        {
            return
                Kind is TokenKind.ConstantString ?
                    $"\"{Value}\"" :
                    Kind is TokenKind.ConstantChar ?
                        $"'{Value}'" :
                        Kind is TokenKind.ConstantFloatDigit && !Value.Contains('.') ?
                            $"{Value}f" :
                            Value;
        }
    }
}
