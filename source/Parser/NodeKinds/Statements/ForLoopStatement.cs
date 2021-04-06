using Mug.Compilation;
using Mug.Models.Lexer;
using System;

namespace Mug.Models.Parser.NodeKinds.Statements
{
    public struct ForLoopStatement : INode
    {
        public string NodeKind => "ForLoop";
        public VariableStatement LeftExpression { get; set; }
        public INode ConditionExpression { get; set; }
        public INode RightExpression { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
