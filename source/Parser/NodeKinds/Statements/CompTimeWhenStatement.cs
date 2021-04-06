using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Models.Parser.NodeKinds.Statements
{
    public class CompTimeWhenStatement : INode
    {
        public string NodeKind => "CompTimeWhen";
        public CompTimeExpression Expression { get; set; }
        public object Body { get; set; } // 
        public ModulePosition Position { get; set; }
    }
}
