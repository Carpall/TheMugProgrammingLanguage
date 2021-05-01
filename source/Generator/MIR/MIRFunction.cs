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

        public override string ToString()
        {
            var locals = new StringBuilder();

            if (Allocations.Length == 0)
                locals.Append(".empty");

            for (int i = 0; i < Allocations.Length; i++)
                locals.AppendFormat(".[{0}] {1}{2}", i, Allocations[i], i < Allocations.Length - 1 ? "\n    " : "");

            return $@".fn {Name}({string.Join(", ", ParameterTypes)}) {ReturnType}:
  .locals:
    {locals}

  {(Body.Length > 0 ? string.Join("\n  ", Body) : ".empty")}
";
        }
    }
}
