using Mug.Compilation;
using Newtonsoft.Json;

namespace Mug.Models.Parser
{
    public interface INode
    {
        public string NodeKind { get; }
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

    public interface IStatement : INode
    {
    }
}
