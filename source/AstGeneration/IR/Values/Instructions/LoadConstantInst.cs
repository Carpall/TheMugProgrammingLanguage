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
    public enum LiquorConstantKind
    {
        Integer,
        FloatingPoint,
        CharPointer,
        String,
        Boolean,
        Character
    }

    public struct LoadConstantInst : ILiquorValue
    {
        public ILiquorType Type { get; }

        public LiquorConstantKind Kind { get; }

        public object Value { get; }

        public ModulePosition Position { get; }

        public LoadConstantInst(LiquorConstantKind kind, object value, ModulePosition position, ILiquorType type = null)
        {
            Kind = kind;
            Value = value;
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        private bool NeedsApexes()
        {
            return Kind is LiquorConstantKind.String or LiquorConstantKind.Character;
        }

        public override string ToString()
        {
            return $"ldc({Kind} {(NeedsApexes() ? $"'{Value}'" : Value)})";
        }
    }
}
