using Mug.Models.Lexer;
using System;
using System.Collections.Generic;

namespace Mug.Compilation
{
    public class CompilationException : Exception
    {
        public MugLexer Lexer { get; }
        public bool IsGlobalError
        {
            get
            {
                return Lexer is null;
            }
        }

        public CompilationException(MugLexer lexer) : this("Cannot build due to previous errors", lexer)
        {
        }

        public CompilationException(string error, MugLexer lexer = null) : base(error)
        {
            Lexer = lexer;
        }
    }
}
