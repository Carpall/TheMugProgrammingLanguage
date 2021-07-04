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
    public struct LiquorComptimeVariable : ILiquorValue
    {
        // public LiquorAccessModifier[]

        public string Name { get; }

        public LiquorBlock Body { get; }

        public ILiquorType Type { get; set; }

        public INode VariableType { get; }

        public ModulePosition Position { get; }

        public LiquorComptimeVariable(string name, LiquorBlock body, ModulePosition position, INode type)
        {
            Name = name;
            Body = body;
            VariableType = type;
            Position = position;
            Type = ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"@'{Name}': {VariableType} = {Body}";
        }
    }
}
