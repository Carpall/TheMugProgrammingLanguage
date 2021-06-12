using Mug.Tokenizer;
using Newtonsoft.Json;
using System;

namespace Mug.Compilation
{
  public struct ModulePosition
    {
        [JsonIgnore]
        public Tokenizer.Lexer Lexer { get; }
        public Range Position { get; }

        public ModulePosition(Tokenizer.Lexer lexer, Range position)
        {
            Lexer = lexer;
            Position = position;
        }

        public override string ToString()
        {
            return $"{Lexer.ModuleName}({Position})";
        }

        public int LineAt()
        {
            return PrettyPrinter.CountLines(Lexer.Source, Position.Start.Value);
        }
    }
}
