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
    public struct CallInst : ILiquorValue
    {
        public ILiquorType Type { get; set; }

        public bool IsBuiltIn { get; }

        public ModulePosition Position { get; }

        public CallInst(bool isBuiltIn, ModulePosition position, ILiquorType type = null)
        {
            IsBuiltIn = isBuiltIn;
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"cll(bltn: {IsBuiltIn})";
        }
    }
}
