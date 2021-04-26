using System;

namespace Mug.Compilation
{
  public class CompilationException : Exception
    {
        public Diagnostic Diagnostic;
        public bool IsGlobalError
        {
            get
            {
                return Diagnostic is null;
            }
        }

        public CompilationException(Diagnostic lexer) : this("Cannot build due to previous errors", lexer)
        {
        }

        public CompilationException(string error, Diagnostic diagnostic = null) : base(error)
        {
            Diagnostic = diagnostic;
        }
    }
}
