using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mug.Syntax.AST
{
    public class FunctionNode : INode
    {
        public string NodeName => "Function";
        public INode Type { get; set; }
        public ParameterNode[] ParameterList { get; set; } = null;
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }

        public IType NodeType { get; set; } = null;

        public bool IsPrototype() => Body is null;

        public override string ToString()
        {
            var parameters = ParameterList is not null ? $"({string.Join(", ", ParameterList)})" : null;
            var type = Type is BadNode ? null : $": {Type}";
            return $"(fn{parameters}{type}{(IsPrototype() ? null : $" {Body}")})";
        }
    }
}
