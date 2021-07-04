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
    public struct LoadLocalAddressInst : ILiquorValue
    {
        public ILiquorType Type { get; set; }

        public string Name { get; }

        public ModulePosition Position { get; }

        public LoadLocalAddressInst(string name, ModulePosition position, ILiquorType type = null)
        {
            Name = name;
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"ldlocaddr({Name})";
        }
    }
}
