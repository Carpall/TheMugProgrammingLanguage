using System;

namespace Nylon.Compilation
{
  public class CompilationException : Exception
    {
        public Diagnostic Diagnostic;
        public bool IsGlobalError
        {
            get
            {
                return
                    Diagnostic is null
                    || Diagnostic.Count == 0
                    || Diagnostic.GetErrors()[0].Bad.Lexer is null;
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
