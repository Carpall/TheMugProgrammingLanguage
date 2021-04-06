using Mug.Models.Lexer;
using System;
using System.Collections.Generic;

namespace Mug.Compilation
{
    public class CompilationException : Exception
    {
        public MugDiagnostic Diagnostic;
        public bool IsGlobalError
        {
            get
            {
                return Diagnostic is null;
            }
        }

        public CompilationException(MugDiagnostic lexer) : this("Cannot build due to previous errors", lexer)
        {
        }

        public CompilationException(string error, MugDiagnostic diagnostic = null) : base(error)
        {
            Diagnostic = diagnostic;
        }
    }
}
