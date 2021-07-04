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
    public struct StoreAddressInst : ILiquorValue
    {
        public ILiquorType Type { get; }

        public ModulePosition Position { get; }

        public StoreAddressInst(ModulePosition position, ILiquorType type = null)
        {
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"staddr()";
        }
    }
}
