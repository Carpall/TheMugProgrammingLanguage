using Mug.Compilation;
using Mug.Models.Lexer;
using System;

namespace Mug.Models.Parser.NodeKinds.Directives
{
    public class DeclareDirective : INode
    {
        public string NodeKind => "DeclareDirective";
        public Token Symbol { get; set; }
        public ModulePosition Position { get; set; }
    }
}
