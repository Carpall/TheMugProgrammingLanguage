using Mug.Compilation;
using Mug.Typing;
using Newtonsoft.Json;

namespace Mug.Syntax.AST
{
    public interface INode
    {
        public string NodeName { get; }

        [JsonIgnore]
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; }

        public string Dump()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public abstract string ToString();
        
        public void TypeNode(IType type)
        {
            NodeType = type;
        }
    }
}
