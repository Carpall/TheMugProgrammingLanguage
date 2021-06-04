using Mug.Compilation;
using Newtonsoft.Json;

namespace Mug.Parser.AST
{
    public interface INode
    {
        public string NodeName { get; }
        [JsonIgnore]
        public abstract ModulePosition Position { get; set; }

        // public abstract string Rebuild();

        public string Dump()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /*public string ToString()
        {
            return Rebuild();
        }*/
    }
}
