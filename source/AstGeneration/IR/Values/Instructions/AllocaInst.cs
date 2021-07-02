using Mug.AstGeneration.IR.Values.Typing;
using Mug.Compilation;
using Mug.Grammar;
using Mug.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR.Values.Instructions
{
    public struct AllocaInst : ILiquorValue
    {
        public ILiquorType Type => ILiquorType.Untyped;

        public INode AllocationType { get; }

        public bool IsMutable { get; }

        public bool IsAssigned { get; }

        public string Name { get; }

        public ModulePosition Position { get; }

        public AllocaInst(string name, bool isAssigned, bool isMutable, ModulePosition position, INode type)
        {
            Name = name;
            IsAssigned = isAssigned;
            IsMutable = isMutable;
            Position = position;
            AllocationType = type;
        }

        public override string ToString()
        {
            return $"allc(isasgn: {IsAssigned}, ismut: {IsMutable}, type: {AllocationType}, name: {Name})";
        }
    }
}
