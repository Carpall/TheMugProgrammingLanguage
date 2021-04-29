using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Models.Generator.IR
{
    public struct MIRFunction
    {
        public string Name { get; }
        public MIRType ReturnType { get; }
        public MIRType[] ParameterTypes { get; }
        public MIRValue[] Body { get; }
        public MIRType[] Allocations { get; }

        public MIRFunction(string name, MIRType returntype, MIRType[] parametertypes, MIRValue[] body, MIRType[] allocations)
        {
            Name = name;
            ReturnType = returntype;
            ParameterTypes = parametertypes;
            Body = body;
            Allocations = allocations;
        }
    }
}
