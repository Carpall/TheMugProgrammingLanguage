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
        public Token DefaultConstantValue { get; }
        public bool IsPassedAsReference { get; }
        public ModulePosition Position { get; set; }

        public ParameterNode(INode type, string name, Token defaultConstValue, bool isPassedAsReference, ModulePosition position)
        {
            Type = type;
            Name = name;
            Position = position;
            DefaultConstantValue = defaultConstValue;
            IsPassedAsReference = isPassedAsReference;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}