using Mug.Models.Lexer;
using System.Collections.Generic;

namespace Mug.Compilation
{
  public class MugDiagnostic
    {
        private readonly List<MugError> _diagnostic = new();

        public int Count => _diagnostic.Count;

        public void Report(MugLexer lexer, int pos, string error)
        {
            Report(new(new(lexer, pos..(pos + 1)), error));
        }

        public void Report(ModulePosition position, string message)
        {
            Report(new MugError(position, message));
        }

        public void Report(MugError error)
        {
            if (!_diagnostic.Contains(error))
                _diagnostic.Add(error);
        }

        public void CheckDiagnostic(MugLexer lexer)
        {
            if (_diagnostic.Count > 0)
                throw new CompilationException(lexer.DiagnosticBag);
        }

        public List<MugError> GetErrors()
        {
            return _diagnostic;
        }
    }
}
