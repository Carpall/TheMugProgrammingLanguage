using Newtonsoft.Json;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public enum MIRAllocationAttribute
    {
        Mutable,
        Unmutable,
    }

    public struct MIRAllocation
    {
        public MIRAllocationAttribute Attributes { get; }
        public MIRType Type { get; }

        public MIRAllocation(MIRAllocationAttribute attributes, MIRType type)
        {
            Attributes = attributes;
            Type = type;
        }

        public override string ToString()
        {
            return $"{{{Attributes}}} {Type}";
        }
    }
}
