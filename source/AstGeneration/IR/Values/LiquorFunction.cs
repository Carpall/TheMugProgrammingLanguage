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
    public struct LiquorFunction : ILiquorValue
    {
        public string Name { get; }

        public ILiquorType Type { get; }

        public ModulePosition Position { get; }

        public LiquorBlock Body { get; }

        public INode[] ParameterTypes { get; }
        
        public INode ReturnType { get; }

        public LiquorFunction(string name, LiquorBlock body, INode[] parameterTypes, INode returntype, ModulePosition position, ILiquorType type = null)
        {
            Name = name;
            Position = position;
            Body = body;
            ParameterTypes = parameterTypes;
            ReturnType = returntype;
            Type = type ?? ILiquorType.Untyped;
        }

        public override string ToString()
        {
            return $"fn({string.Join<INode>(", ", ParameterTypes)}): {ReturnType} {Body}";
        }
    }
}
