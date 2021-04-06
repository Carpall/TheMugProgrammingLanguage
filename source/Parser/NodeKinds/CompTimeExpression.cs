using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Models.Parser.NodeKinds
{
    public class CompTimeExpression : INode
    {
        public string NodeKind => "CompTimeWhen";
        public List<Token> Expression { get; set; } = new();
        public ModulePosition Position { get; set; }
    }
}
