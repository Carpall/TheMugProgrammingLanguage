using Mug.AstGeneration.IR.Values.Typing;
using Mug.Compilation;
using Mug.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR.Values.Instructions
{

    public struct JumpInst : ILiquorValue
    {
        public ILiquorType Type => ILiquorType.Untyped;

        public LabelInst Label { get; }

        public ModulePosition Position { get; }

        public JumpInst(LabelInst label)
        {
            Label = label;
            Position = new();
        }

        public override string ToString()
        {
            return $"jmp({Label})";
        }
    }
}
