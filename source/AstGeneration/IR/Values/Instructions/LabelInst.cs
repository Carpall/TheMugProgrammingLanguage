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

    public class LabelInst : ILiquorValue
    {
        public ILiquorType Type => ILiquorType.Untyped;

        public int Index { get; set; } = -1;

        public ModulePosition Position { get; }

        public override string ToString()
        {
            return $"L{Index}:";
        }
    }
}
