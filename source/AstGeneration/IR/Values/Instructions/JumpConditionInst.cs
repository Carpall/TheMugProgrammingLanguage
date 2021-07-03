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
    public struct JumpConditionInst : ILiquorValue
    {
        public ILiquorType Type => ILiquorType.Untyped;

        public bool Condition { get; }

        public LabelInst Label { get; }

        public ModulePosition Position { get; }

        public JumpConditionInst(LabelInst label, bool condition)
        {
            Label = label;
            Condition = condition;
            Position = new();
        }

        public override string ToString()
        {
            return $"jmp{Condition.ToString().ToLower()}({Label})";
        }
    }
}
