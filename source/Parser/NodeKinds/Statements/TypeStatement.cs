using Mug.Compilation;
using Mug.Models.Lexer;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds.Statements
{
  public class TypeStatement : INode
    {
        public string NodeKind => "Struct";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<Token> Generics { get; set; } = new();
        public List<FieldNode> Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
    }
}
