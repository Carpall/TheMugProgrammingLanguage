using Mug.Compilation;
using Mug.Grammar;
using System.Collections.Generic;

namespace Mug.Syntax.AST
{
    public class StructureNode : INode
    {
        public string NodeName => "Struct";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<Token> Generics { get; set; } = new();
        public List<FieldNode> BodyFields { get; set; } = new();
        public List<FunctionNode> BodyMethods { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsPacked => Pragmas.PragmaIsTrue("packed");

        public override string ToString()
        {
            return Name;
        }
    }
}
