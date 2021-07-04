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
    public struct LoadFieldInst : ILiquorValue
    {
        public ILiquorType Type { get; }

        public string Name { get; }

        public ModulePosition Position { get; }

        public LoadFieldInst(string name, ModulePosition position, ILiquorType type = null)
        {
            Name = name;
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"ldfld({Name})";
        }
    }
}
