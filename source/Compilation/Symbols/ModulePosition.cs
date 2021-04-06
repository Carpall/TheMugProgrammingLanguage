using Mug.Models.Lexer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Compilation.Symbols
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
    }
}
