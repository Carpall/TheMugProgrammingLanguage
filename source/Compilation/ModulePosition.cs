using Mug.Models.Lexer;
using Newtonsoft.Json;
using System;

namespace Mug.Compilation
{
  public struct ModulePosition
    {
        [JsonIgnore]
        public Lexer Lexer { get; }
        public Range Position { get; }

        public ModulePosition(Lexer lexer, Range position)
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
