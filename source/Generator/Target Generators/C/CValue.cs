using Mug.Generator.IR;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.TargetGenerators.C
{
    struct CValue
    {
        public DataType Type { get; }
        public string Value { get; }

        public CValue(DataType type, string value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
