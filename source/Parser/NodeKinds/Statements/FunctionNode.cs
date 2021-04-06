using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds.Statements
{
    public class FunctionNode : INode
    {
        public string NodeKind => "Function";
        public Pragmas Pragmas { get; set; }
        public string Name { get; set; }
        public MugType ReturnType { get; set; }
        public ParameterListNode ParameterList { get; set; } = new();
        public List<Token> Generics { get; set; } = new();
        public BlockNode Body { get; set; } = new();
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }
        public ParameterNode? Base { get; set; }

        public override string ToString()
        {
            var parameters = new MugType[ParameterList.Length];
            var i = 0;
            ParameterList.Parameters.ForEach(parameter =>
            {
                // avoid ambiguity between generic type and defined type
                parameters[i] = IsGenericParameter(parameter.Type.ToString(), out var index) ? new MugType(new(), TypeKind.DefinedType, "@"+index) : parameter.Type;
                i++;
            });

            return $"{(Base.HasValue ? $"{Base.Value.Type}." : "")}{Name}<{Generics.Count}>({string.Join<MugType>(", ", parameters)})";
        }

        private bool IsGenericParameter(string name, out int i)
        {
            for (i = 0; i < Generics.Count; i++)
                if (Generics[i].Value == name)
                    return true;

            return false;
        }
    }
}
