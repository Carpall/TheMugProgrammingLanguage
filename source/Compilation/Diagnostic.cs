using Nylon.Models.Lexer;
using System;
using System.Collections.Generic;

namespace Nylon.Compilation
{
  public class Diagnostic
    {
        private readonly List<CompilationError> _diagnostic = new();

        public int Count => _diagnostic.Count;

        public void Report(Lexer lexer, int pos, string error)
        {
            Report(new(new(lexer, pos..(pos + 1)), error));
        }

        public void Report(ModulePosition position, string message)
        {
            Report(new CompilationError(position, message));
        }

        public void Report(CompilationError error)
        {
            if (!_diagnostic.Contains(error))
                _diagnostic.Add(error);
        }

        public void CheckDiagnostic()
        {
            if (_diagnostic.Count > 0)
                throw new CompilationException(this);
        }

        public List<CompilationError> GetErrors()
        {
            return _diagnostic;
        }

        internal void AddRange(Diagnostic diagnostic)
        {
            _diagnostic.AddRange(diagnostic._diagnostic);
        }
    }
}
