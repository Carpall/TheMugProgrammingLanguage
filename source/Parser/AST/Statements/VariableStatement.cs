using Mug.Compilation;
using Mug.TypeSystem;

namespace Mug.Models.Parser.AST.Statements
{
    public class VariableStatement : INode
    {
        public string NodeKind => "Var";
        public string Name { get; set; }
        public MugType Type { get; set; }
        public bool IsAssigned
        {
            get
            {
                return Body is not null;
            }
        }
        public INode Body { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsConst { get; set; }
    }
}
