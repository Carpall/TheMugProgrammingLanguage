using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public struct MIRStructure
    {
        public bool IsPacked { get; }
        public string Name { get; }
        public MIRType[] Body { get; }

        public MIRStructure(
            bool isPacked,
            string name,
            MIRType[] body)
        {
            IsPacked = isPacked;
            Name = name;
            Body = body;
        }

        public override string ToString()
        {
            var body = GetBodyReppresentation();

            return $@".strct {(IsPacked ? "packed " : "")}{Name}:
  .fields
    {body}
";
        }

        private readonly string GetBodyReppresentation()
        {
            var body = new StringBuilder();

            if (Body.Length == 0)
                body.Append(".empty");

            body.Append(string.Join("\n    ", Body));

            return body.ToString();
        }
    }
}
