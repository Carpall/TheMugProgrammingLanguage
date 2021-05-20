using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST
{
    public struct ParameterNode : INode
    {
        public string NodeKind => "Parameter";
        public DataType Type { get; }
        public string Name { get; }
        public Token DefaultConstantValue { get; }
        public bool IsPassedAsReference { get; }
        public ModulePosition Position { get; set; }

        public ParameterNode(DataType type, string name, Token defaultConstValue, bool isPassedAsReference, ModulePosition position)
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

    public class ParameterListNode : INode
    {
        public string NodeKind => "ParameterList";
        public ModulePosition Position { get; set; }
        public int Length
        {
            get
            {
                return Parameters.Count;
            }
        }

        public readonly List<ParameterNode> Parameters = new();
    }
}