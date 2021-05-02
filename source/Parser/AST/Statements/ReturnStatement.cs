using Mug.Compilation;

namespace Mug.Models.Parser.AST.Statements
{
  public class ReturnStatement : INode
    {
        public string NodeKind => "Return";
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsVoid()
        {
            return Body is null;
        }
    }
}
