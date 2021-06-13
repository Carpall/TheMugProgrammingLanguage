using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.TargetGenerators.C
{
    class CStructureBuilder
    {
        public string Prototype { get; }
        public List<string> Body { get; } = new();

        public CStructureBuilder(string prototype)
        {
            Prototype = prototype;
        }

        public override string ToString()
        {
            return $"{Prototype} {{\n{string.Join("\n", Body)}\n}}";
        }
    }
}
