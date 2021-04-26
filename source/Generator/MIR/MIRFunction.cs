using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public struct MIRFunction
    {
        public string Name { get; }
        public SolvedType ReturnType { get; }
        public SolvedType[] ParameterTypes { get; }
        public IMirstruction[] Body { get; }
    }
}
