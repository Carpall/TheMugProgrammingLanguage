using Mug.Generator.IR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public class MIRBlock
    {
        public int Index { get; set; }
        public string Identifier { get; }
        public List<MIRInstruction> Instructions { get; }
        public List<int> ReferredFrom { get; } = new();

        public MIRBlock(int index)
        {
            Index = index;
        }

        public MIRBlock(int index, List<MIRInstruction> instructions) : this(index)
        {
            Instructions = instructions;
        }

        public MIRBlock(int index, string identifier) : this(index, new List<MIRInstruction>())
        {
            Identifier = identifier;
        }

        public override string ToString()
        {
            return $"{Identifier}:\n  {string.Join("\n  ", Instructions)}";
        }
    }
}
