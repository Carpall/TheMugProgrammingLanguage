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
        public DataType ReturnType { get; }
        public DataType[] ParameterTypes { get; }
        public MIRInstruction[] Body { get; }
        public MIRAllocation[] Allocations { get; }
        public MIRLabel[] Labels { get; }

        public MIRFunction(
            string name,
            DataType returntype,
            DataType[] parametertypes,
            MIRInstruction[] body,
            MIRAllocation[] allocations,
            MIRLabel[] labels)
        {
            Name = name;
            ReturnType = returntype;
            ParameterTypes = parametertypes;
            Body = body;
            Allocations = allocations;
            Labels = labels;
        }

        public override string ToString()
        {
            var locals = GetAllocationsReppresentation();
            var body = GetBodyReppresentation();

            return $@".fn {Name}({string.Join<DataType>(", ", ParameterTypes)}) {ReturnType}:
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

            for (var i = 0; i < Body.Length; i++)
                body.AppendFormat("L{0}: {1}{2}", i, Body[i], i < Body.Length - 1 ? "\n  " : "");

            return body.ToString();
        }
    }
}
