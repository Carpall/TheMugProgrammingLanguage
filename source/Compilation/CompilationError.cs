namespace Zap.Compilation
{
  public struct CompilationError
    {
        public ModulePosition Bad { get; }
        public string Message { get; }

        public CompilationError(ModulePosition position, string message)
        {
            Bad = position;
            Message = message;
        }
    }
}
