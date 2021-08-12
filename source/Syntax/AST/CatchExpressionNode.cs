using Mug.Compilation;
using Mug.Grammar;
using Mug.Typing;

namespace Mug.Syntax.AST
{
    public class CatchExpressionNode : INode
    {
        public string NodeName => "Catch";
        public INode Expression { get; set; }
        public BlockNode Body { get; set; }
        public Token? OutError { get; set; }
        public ModulePosition Position { get; set; }

        

        public override string ToString()
        {
            return $"{Expression} catch {(OutError.HasValue ? $"{OutError} " : null)}{Body}";
        }
    }
}