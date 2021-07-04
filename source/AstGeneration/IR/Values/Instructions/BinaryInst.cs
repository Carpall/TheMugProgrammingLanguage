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
    public enum LiquorBinaryInstKind
    {
        Add,
        Sub,
        Mul,
        Div,
        Eq,
        Ne,
        Gt,
        Lt,
        Ge,
        Le
    }

    public struct BinaryInst : ILiquorValue
    {
        public ILiquorType Type { get; set; }

        public LiquorBinaryInstKind Kind { get; }

        public ModulePosition Position { get; }

        public BinaryInst(LiquorBinaryInstKind kind, ModulePosition position, ILiquorType type = null)
        {
            Kind = kind;
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"{Kind.ToString().ToLower()}()";
        }
    }
}
