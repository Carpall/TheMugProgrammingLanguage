using Nylon.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.Models.Generator.IR
{
    public struct NIRFunction
    {
        public string Name { get; }
        public DataType ReturnType { get; }
        public DataType[] ParameterTypes { get; }
        public NIRValue[] Body { get; }
        public NIRAllocation[] Allocations { get; }

        public NIRFunction(
            string name,
            DataType returntype,
            DataType[] parametertypes,
            NIRValue[] body,
            NIRAllocation[] allocations)
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

            return $@".fn {Name}({string.Join<DataType>(", ", ParameterTypes)}) {ReturnType}:
  .locals:
    {locals}

  {(Body.Length > 0 ? string.Join("\n  ", Body) : ".empty")}
";
        }
    }
}
