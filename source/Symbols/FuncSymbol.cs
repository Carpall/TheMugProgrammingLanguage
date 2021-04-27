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
    public class FuncSymbol : ISymbol
    {
        internal FunctionStatement Func { get; }
        public ModulePosition Position => Func.Position;

        public FuncSymbol(FunctionStatement func)
        {
            Func = func;
        }

        public string Dump(bool dumpmodel)
        {
            var result = new StringBuilder($"func {Func.Name}");

            if (!dumpmodel)
                return result.ToString();

            if (Func.Generics.Count > 0)
            {
                result.Append('<');
                for (int i = 0; i < Func.Generics.Count; i++)
                {
                    if (i > 0)
                        result.Append(", ");
                    result.Append($"{Func.Generics[i].Value}");
                }
                result.Append('>');
            }

            result.Append('(');

            for (int i = 0; i < Func.ParameterList.Length; i++)
            {
                var parameter = Func.ParameterList.Parameters[i];

                if (i > 0)
                    result.Append(", ");
                result.Append($"{parameter.Name}: {parameter.Type}");
            }

            result.Append("): ");
            result.AppendLine(Func.ReturnType.ToString());
            return result.ToString();
        }

        public override string ToString()
        {
            return Dump(false);
        }
    }
}
