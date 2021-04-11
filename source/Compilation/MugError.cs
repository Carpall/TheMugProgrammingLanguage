using Mug.Compilation.Symbols;
using Mug.Models.Lexer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Compilation
{
    public struct MugError
    {
        public ModulePosition Bad { get; }
        public string Message { get; }
        public int LineAt(string source)
        {
            return CompilationErrors.CountLines(source, Bad.Position.Start.Value);
        }

        public MugError(ModulePosition position, string message)
        {
            Bad = position;
            Message = message;
        }
    }
}
