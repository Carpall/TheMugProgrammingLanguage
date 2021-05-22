using Mug.Models.Lexer;
using System;
using System.Collections.Generic;

namespace Mug.Compilation
{
  public class Diagnostic
    {
        private readonly List<CompilationAlert> _diagnostic = new();
        private int _otherAlertsCount = 0;

        public int Count => _diagnostic.Count - _otherAlertsCount;


        public void Report(Lexer lexer, int pos, string error)
        {
            Report(new(CompilationAlertKind.Error, new(lexer, pos..(pos + 1)), error));
        }

        public void Report(ModulePosition position, string message)
        {
            Report(new(CompilationAlertKind.Error, position, message));
        }

        public void Report(CompilationAlert error)
        {
            if (!_diagnostic.Contains(error))
                _diagnostic.Add(error);
        }

        public void Warn(ModulePosition position, string message)
        {
            _otherAlertsCount++;
            _diagnostic.Add(new(CompilationAlertKind.Warning, position, message));
        }

        public void CheckDiagnostic()
        {
            if (Count > 0)
                throw new CompilationException(this);
        }

        public List<CompilationAlert> GetAlerts()
        {
            return _diagnostic;
        }

        internal void AddRange(Diagnostic diagnostic)
        {
            _otherAlertsCount += diagnostic._otherAlertsCount;
            _diagnostic.AddRange(diagnostic._diagnostic);
        }

        internal void RestoreTo(int errors)
        {
            _diagnostic.RemoveRange(errors, Count - errors);
        }
    }
}
