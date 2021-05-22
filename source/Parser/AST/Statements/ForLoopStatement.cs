using Mug.Compilation;

namespace Mug.Models.Parser.AST.Statements
{
  public struct ForLoopStatement : INode
    {
        public string NodeName => "ForLoop";
        public VariableStatement LeftExpression { get; set; }
        public INode ConditionExpression { get; set; }
        public INode RightExpression { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }
    }
}
