using Mug.AstGeneration.IR.Values;
using Mug.AstGeneration.IR.Values.Typing;
using Mug.Compilation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR
{
    public class LiquorBlock : ILiquorValue
    {
        internal static string Indent = "";

        public List<ILiquorValue> Instructions { get; } = new();

        public LiquorBlock(ILiquorType type = null)
        {
            Type = type ?? ILiquorType.Untyped;
        }

        public ILiquorType Type { get; }

        public ModulePosition Position { get; }

        public override string ToString()
        {
            Indent += "  ";
            var result = new StringBuilder("{\n");

            for (int i = 0; i < Instructions.Count; i++)
                result.AppendLine($"{Indent}{Instructions[i]}");

            Indent = Indent[..^2];
            result.Append($"{Indent}}}");
            return result.ToString();
        }
    }
}
