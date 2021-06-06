using Mug.Generator.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Generator.TargetGenerators.C
{
    class CFunctionBuilder
    {
        public int ParametersCount { get; }
        public string Prototype { get; }
        public List<List<string>> Body { get; } = new();
        public List<MIRType> Allocations { get; } = new();
        public List<string> CurrentBlock => Body[^1];

        public CFunctionBuilder(string prototype, int parametersCount)
        {
            Prototype = prototype;
            ParametersCount = parametersCount;
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            for (int i = 0; i < Allocations.Count; i++)
                result.AppendLine($"  {Allocations[i]} A{i + ParametersCount};");

            for (int i = 0; i < Body.Count; i++)
            {
                result.AppendLine($" L{i}:;");
                result.AppendLine("  " + string.Join("\n  ", Body[i]));
            }

            return $"{Prototype} {{\n{result}\n}}";
        }
    }
}
