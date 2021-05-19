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
                    Message != "";
            }
        }

        public CompilationException(Diagnostic diagnostic) : base("")
        {
            Diagnostic = diagnostic;
        }

        public CompilationException(string error) : base(error)
        {
        }
    }
}
