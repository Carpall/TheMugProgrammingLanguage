using Mug.AstGeneration.IR.Values.Typing;
using Mug.Compilation;
using Mug.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR.Values
{
    public struct LiquorVariable : ILiquorValue
    {
        // public LiquorAccessModifier[]

        public string Name { get; }

        public ILiquorValue Body { get; }

        public ILiquorType Type => ILiquorType.Untyped;

        public INode VariableType { get; }

        public ModulePosition Position { get; }

        public LiquorVariable(string name, ILiquorValue body, ModulePosition position, INode type)
        {
            Name = name;
            Body = body;
            VariableType = type;
            Position = position;
        }

        public override string ToString()
        {
            return $"@'{Name}': {Type} = {Body}";
        }
    }
}
