using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;
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

        public INode Type;

        public INode Body;

        public ModulePosition Position { get; set; }

        public bool IsConst { get; set; }
        public bool IsMutable { get; set; }
        public bool IsLocal { get; set; }

        public IType NodeType { get; set; } = null;

        public bool IsAssigned() => !(Body is BadNode or null);
        public bool IsAuto() => Type is BadNode;

        internal int DeclarationIndex = -1;

        public override string ToString()
        {
            var keyword = IsConst ? "const" : $"let{(IsMutable ? $" mut" : null)}";
            return $"{Modifiers.Format()}{keyword} {Name}{(IsAuto() ? null : $": {Type}")} = {Body}";
        }
    }
}
