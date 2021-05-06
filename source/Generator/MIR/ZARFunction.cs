using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Models.Generator.IR
{
    public struct ZARFunction
    {
        public string Name { get; }
        public ZapType ReturnType { get; }
        public ZapType[] ParameterTypes { get; }
        public ZARValue[] Body { get; }
        public ZapType[] Allocations { get; }

        public ZARFunction(string name, ZapType returntype, ZapType[] parametertypes, ZARValue[] body, ZapType[] allocations)
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

            return $@".fn {Name}({string.Join<ZapType>(", ", ParameterTypes)}) {ReturnType}:
  .locals:
    {locals}

  {(Body.Length > 0 ? string.Join("\n  ", Body) : ".empty")}
";
        }
    }
}
