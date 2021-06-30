using Mug.Compilation;
using Mug.Grammar;

using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public struct ParameterNode : INode
    {
        public string NodeName => "Parameter";
        public INode Type { get; set; }
        public string Name { get; }
        public INode DefaultConstantValue { get; }
        public bool IsStatic { get; set; }
        public ModulePosition Position { get; set; }

        public ParameterNode(INode type, string name, INode defaultConstValue, bool isStatic, ModulePosition position)
        {
            Type = type;
            Name = name;
            Position = position;
            DefaultConstantValue = defaultConstValue;
            IsStatic = isStatic;
        }

        public override string ToString()
        {
            return $"{(IsStatic ? "static " : null)}{Name}: {Type}";
        }
    }
}