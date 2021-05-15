namespace Nylon.Compilation
{
    public enum CompilationAlertKind
    {
        Error,
        Warning,
        Note
    }

    public struct CompilationAlert
    {
        public CompilationAlertKind Kind { get; }
        public ModulePosition Bad { get; }
        public string Message { get; }

        public CompilationAlert(CompilationAlertKind kind, ModulePosition position, string message)
        {
            Kind = kind;
            Bad = position;
            Message = message;
        }
    }
}
