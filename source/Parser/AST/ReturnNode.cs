using Mug.Compilation;

namespace Mug.Syntax.AST
{
  public class ReturnNode : INode
    {
        public string NodeName => "Return";
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsVoid()
        {
            return Body is BadNode;
        }

        public override string ToString()
        {
            return $"return{(IsVoid() ? null : Body)}";
        }
    }
}
