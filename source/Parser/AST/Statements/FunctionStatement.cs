using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Symbols;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.AST.Statements
{
    public class FunctionStatement : INode, ISymbol
    {
        public string NodeName => "Function";
        public Pragmas Pragmas { get; set; }
        public TokenKind Modifier { get; set; }
        public string Name { get; set; }
        public DataType ReturnType { get; set; }
        public ParameterListNode ParameterList { get; set; } = new();
        public List<Token> Generics { get; set; } = new();
        public BlockNode Body { get; set; } = new();
        public ModulePosition Position { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
