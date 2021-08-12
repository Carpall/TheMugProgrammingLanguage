using Mug.Backend.IR.Types;
using Mug.Backend.IR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Backend.IR
{
    public class MugIR
    {
        public string Filename { get; }

        public List<(string Name, IRValue Type, IRValue Value)> Functions { get; } = new();

        public MugIR(string name)
        {
            Filename = name;
        }

        public override string ToString()
        {
            var result = new StringBuilder($"!filename = '{Filename}'\n\n");
            foreach (var variable in Functions)
                result.AppendLine($"%{variable.Name} -> {variable.Type} = {variable.Value};");

            return result.ToString();
        }
    }
}