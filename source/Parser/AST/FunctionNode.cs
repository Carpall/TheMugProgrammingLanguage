using Mug.Compilation;
using Mug.Grammar;

using System;
using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class FunctionNode : INode
    {
        public string NodeName => "Function";
        public Pragmas Pragmas { get; set; }
        public INode ReturnType { get; set; }
        public ParameterNode[] ParameterList { get; set; } = Array.Empty<ParameterNode>();
        public TokenKind Modifier { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }

        public bool IsPrototype => Body is null;
    }
}
