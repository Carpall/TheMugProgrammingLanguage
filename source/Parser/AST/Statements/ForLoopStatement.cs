using Zap.Compilation;

namespace Zap.Models.Parser.AST.Statements
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
