using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public struct MIRGlobal
    {
        public string Name { get; }
        public DataType Type { get; }

        public MIRGlobal(string name, DataType type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return $".glb {Type} {Name}";
        }
    }
}
