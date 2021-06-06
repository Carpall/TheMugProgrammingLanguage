using Mug.Generator.IR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.TargetGenerators.C
{
    struct CValue
    {
        public MIRType Type { get; }
        public string Value { get; }

        public CValue(MIRType type, string value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"({Type}){Value}";
        }
    }
}
