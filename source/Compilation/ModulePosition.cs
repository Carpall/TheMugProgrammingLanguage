using Mug.Models.Lexer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Compilation
{
    public struct ModulePosition
    {
        public MugLexer Lexer { get; }
        public Range Position { get; }

        public ModulePosition(MugLexer lexer, Range position)
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
            return CompilationErrors.CountLines(Lexer.Source, Position.Start.Value);
        }
    }
}
