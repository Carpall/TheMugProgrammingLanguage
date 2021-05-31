using Mug.TypeSystem;

namespace Mug.Generator
{
    public class MIRLabel
    {
        public int BodyIndex { get; set; }
        public string Name { get; set; }

        public MIRLabel(int bodyIndex, string name)
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
