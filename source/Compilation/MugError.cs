namespace Mug.Compilation
{
  public struct MugError
    {
        public ModulePosition Bad { get; }
        public string Message { get; }

        public MugError(ModulePosition position, string message)
        {
            Bad = position;
            Message = message;
        }
    }
}
