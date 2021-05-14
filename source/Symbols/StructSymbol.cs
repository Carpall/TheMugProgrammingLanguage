using Nylon.Compilation;
using Nylon.Models.Parser.AST.Statements;
using Nylon.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.Symbols
{
    public class StructSymbol : ISymbol
    {
        internal TypeStatement Type { get; }
        public ModulePosition Position => Type.Position;

        public StructSymbol(TypeStatement type)
        {
            Type = type;
        }

        public string Dump(bool dumpmodel)
        {
            if (!dumpmodel)
                return Type.Name;

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

            foreach (var field in Type.BodyFields)
                result.AppendLine($"  {field.Name}: {field.Type}");

            result.AppendLine("}");
            return result.ToString();
        }

        public override string ToString()
        {
            return Dump(false);
        }
    }
}
