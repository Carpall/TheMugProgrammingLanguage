using Newtonsoft.Json;
using Nylon.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.Models.Generator.IR
{
    public enum NIRAllocationAttribute
    {
        Mutable,
        Unmutable,
        HiddenBuffer,
    }

    public struct NIRAllocation
    {
        public NIRAllocationAttribute Attributes { get; }
        public DataType Type { get; }

        public NIRAllocation(NIRAllocationAttribute attributes, DataType type)
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
