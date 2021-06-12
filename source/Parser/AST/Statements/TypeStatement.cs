using Mug.Compilation;
using Mug.Generator.IR;
using Mug.Tokenizer;
using Mug.Symbols;
using System;
using System.Collections.Generic;

namespace Mug.Parser.AST.Statements
{
    public class TypeStatement : INode, ISymbol
    {
        public string NodeName => "Struct";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public List<Token> Generics { get; set; } = new();
        public List<FieldNode> BodyFields { get; set; } = new();
        public List<FunctionStatement> BodyMethods { get; set; } = new();
        public TokenKind Modifier { get; set; }
        public ModulePosition Position { get; set; }
        public bool IsPacked => Pragmas.PragmaIsTrue("packed");
        internal MIRStructure? LoweredStructCache { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public void CacheLoweredStruct(MIRStructure MIRStructure)
        {
            LoweredStructCache = MIRStructure;
        }
    }
}
