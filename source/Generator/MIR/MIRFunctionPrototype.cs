using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public struct MIRFunctionPrototype
    {
        public string Name { get; }
        public MIRType ReturnType { get; }
        public MIRType[] ParameterTypes { get; }

        public MIRFunctionPrototype(
            string name,
            MIRType returntype,
            MIRType[] parametertypes)
        {
            Name = name;
            ReturnType = returntype;
            ParameterTypes = parametertypes;
        }

        public override string ToString()
        {
            return $@".fn {Name}({string.Join(", ", ParameterTypes)}) {ReturnType}";
        }
    }
}
