using Nylon.Models.Lexer;
using System;

namespace Nylon.Compilation
{
  public struct ModulePosition
    {
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
