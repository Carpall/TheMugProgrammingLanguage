using Mug.Compilation;
using Mug.Grammar;
using System;
using System.Collections.Immutable;

namespace Mug.Syntax.AST
{
    public class VariableNode : INode
    {
        public ImmutableArray<TokenKind> Modifiers { get; set; }
        public Pragmas Pragmas { get; set; }

        public string NodeName => "Var";
        
        public string Name { get; set; }

        public INode Type { get; set; }

        public INode Body { get; set; }

        public ModulePosition Position { get; set; }

        public bool IsConst { get; set; }
        public bool IsMutable { get; set; }

        public bool IsAssigned() => Body is not BadNode;
        public bool IsAuto() => Type is BadNode;

        public override string ToString()
        {
            var keyword = IsConst ? "const" : $"let{(IsMutable ? $" mut" : null)}";
            return $"{Modifiers.Format()}{keyword} {Name}{(IsAuto() ? null : $": {Type}")} = {Body}";
        }
    }
}
