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
        public MIRFunctionPrototype Prototype { get; }
        public MIRBlock[] Body { get; }
        public MIRAllocation[] Allocations { get; }

        public MIRFunction(
            string name,
            DataType returntype,
            DataType[] parametertypes,
            MIRBlock[] body,
            MIRAllocation[] allocations)
        {
            Prototype = new(name, returntype, parametertypes);
            Body = body;
            Allocations = allocations;
        }

        public override string ToString()
        {
            var locals = GetAllocationsReppresentation();
            var body = GetBodyReppresentation();

            return $@"{Prototype}:
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

            for (int i = 0; i < Body.Length; i++)
            {
                if (i > 0)
                    body.AppendLine("\n");

                body.Append($" [{i}] % {Body[i]}");
            }

            return body.ToString();
        }
    }
}
