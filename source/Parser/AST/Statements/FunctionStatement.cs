using Mug.Compilation;
using Mug.Tokenizer;
using Mug.Symbols;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;

namespace Mug.Parser.AST.Statements
{
    public class FunctionStatement : INode, ISymbol
    {
        public string NodeName => "Function";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public DataType ReturnType { get; set; }
        public ParameterNode[] ParameterList { get; set; } = Array.Empty<ParameterNode>();
        public List<Token> Generics { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public BlockNode Body { get; set; }
        public ModulePosition Position { get; set; }

        public bool IsPrototype => Body is null;

        public override string ToString()
        {
            return Name;
        }
    }
}
