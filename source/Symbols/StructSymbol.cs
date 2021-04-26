using Mug.Compilation;
using Mug.Models.Parser.AST.Statements;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Symbols
{
    public class StructSymbol : ISymbol
    {
        public string Description => "type";
        internal TypeStatement Type { get; }
        public ModulePosition Position => Type.Position;

        public StructSymbol(TypeStatement type)
        {
            Type = type;
        }

        public override string ToString()
        {
            var result = new StringBuilder($"type {Type.Name}");

            if (Type.Generics.Count > 0)
            {
                result.Append('<');
                for (int i = 0; i < Type.Generics.Count; i++)
                {
                    if (i > 0)
                        result.Append(", ");
                    result.Append($"{Type.Generics[i].Value}");
                }
                result.Append('>');
            }

            result.Append(" {\n");

            foreach (var field in Type.Body)
                result.AppendLine($"  {field.Name}: {field.Type}");

            result.Append('}');
            return result.ToString();
        }
    }
}
