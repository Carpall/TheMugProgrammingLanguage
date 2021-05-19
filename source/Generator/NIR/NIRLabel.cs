using Nylon.TypeSystem;

namespace Nylon.Models.Generator
{
    public class NIRLabel
    {
        public int BodyIndex { get; set; }
        public string Name { get; set; }

        public NIRLabel(int bodyIndex, string name)
        {
            BodyIndex = bodyIndex;
            Name = name;
        }

        public override string ToString()
        {
            return $"L{BodyIndex} {Name}";
        }
    }
}
