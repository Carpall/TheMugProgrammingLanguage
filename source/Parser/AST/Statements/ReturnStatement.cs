using Mug.Compilation;

namespace Mug.Parser.AST.Statements
{
  public class ReturnStatement : INode
    {
        public string NodeName => "Return";
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsVoid()
        {
            return Body is null;
        }
    }
}
