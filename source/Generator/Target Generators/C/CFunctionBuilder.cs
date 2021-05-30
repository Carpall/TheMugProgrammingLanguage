using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.TargetGenerators.C
{
    class CFunctionBuilder
    {
        public string Prototype { get; }
        public StringBuilder Body { get; } = new();

        public CFunctionBuilder(string prototype)
        {
            Prototype = prototype;
        }

        public override string ToString()
        {
            return $"{Prototype} {{\n{Body}\n}}";
        }
    }
}
