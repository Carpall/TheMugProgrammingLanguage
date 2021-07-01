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
        public ILiquorType Type { get; }

        public string Name { get; }

        public bool IsInstance { get; }

        public bool IsBuiltIn { get; }

        public ModulePosition Position { get; }

        public CallInst(string name, bool isInstance, bool isBuiltIn, ModulePosition position, ILiquorType type = null)
        {
            Name = name;
            IsInstance = isInstance;
            IsBuiltIn = isBuiltIn;
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"cll(bltn: {IsBuiltIn}, instnc: {IsInstance}, name: {Name})";
        }
    }
}
