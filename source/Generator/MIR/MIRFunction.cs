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
        public MIRInstruction[] Body { get; }
        public MIRAllocation[] Allocations { get; }

        public MIRFunction(
            string name,
            MIRType returntype,
            MIRType[] parametertypes,
            MIRInstruction[] body,
            MIRAllocation[] allocations)
        {
            Name = name;
            ReturnType = returntype;
            ParameterTypes = parametertypes;
            Body = body;
            Allocations = allocations;
        }

        public override string ToString()
        {
            var locals = GetAllocationsReppresentation();
            var body = GetBodyReppresentation();

            return $@".fn {Name}({string.Join(", ", ParameterTypes)}) {ReturnType}:
  .locals:
    {locals}

  {body}
";
        }

        private readonly string GetAllocationsReppresentation()
        {
            var locals = new StringBuilder();

            if (Allocations.Length == 0)
                locals.Append(".empty");

            for (var i = 0; i < Allocations.Length; i++)
                locals.AppendFormat(".[{0}] {1}{2}", i, Allocations[i], i < Allocations.Length - 1 ? "\n    " : "");

            return locals.ToString();
        }

        private readonly string GetBodyReppresentation()
        {
            var body = new StringBuilder();

            if (Body.Length == 0)
                body.Append(".empty");

            body.Append(string.Join("\n  ", Body));

            return body.ToString();
        }
    }
}
