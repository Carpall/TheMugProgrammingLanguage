using Mug.Compilation;
using Newtonsoft.Json;

namespace Mug.Syntax.AST
{
    public interface INode
    {
        public string NodeName { get; }
        [JsonIgnore]
        public abstract ModulePosition Position { get; set; }

        public string Dump()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public abstract string ToString();
    }
}
