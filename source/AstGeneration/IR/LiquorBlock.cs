using Mug.AstGeneration.IR.Values;
using Mug.AstGeneration.IR.Values.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR
{
    public struct LiquorBlock
    {
        private static string _indent = "";

        public List<ILiquorValue> Instructions;

        public LiquorBlock(List<ILiquorValue> instructions)
        {
            Instructions = instructions;
        }

        public override string ToString()
        {
            _indent += "  ";
            var result = new StringBuilder("{\n");

            foreach (var instruction in Instructions)
                result.AppendLine($"{_indent}{instruction.WriteToString()}");

            _indent = _indent[..^2];
            result.Append($"{_indent}}}");
            return result.ToString();
        }
    }
}
