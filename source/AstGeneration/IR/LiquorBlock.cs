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
    public class BlockBranch : IEnumerable<ILiquorValue>
    {
        private readonly List<ILiquorValue> Instructions = new();

        public void Add(ILiquorValue inst)
        {
            Instructions.Add(inst);
        }

        public IEnumerator<ILiquorValue> GetEnumerator()
        {
            return Instructions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string WriteToString(int i)
        {
            var result = new StringBuilder();

            foreach (var instruction in Instructions)
                result.AppendLine($"{LiquorBlock.Indent}{instruction?.WriteToString()}");

            return $"{LiquorBlock.Indent[..^1]}[{i}] br % -> \n{result}";
        }
    }

    public struct LiquorBlock : ILiquorValue
    {
        internal static string Indent = "";

        public List<BlockBranch> Branches { get; }

        public BlockBranch CurrentBranch => Branches.Last();

        public LiquorBlock(List<BlockBranch> branches, ModulePosition position, ILiquorType type = null)
        {
            Branches = branches;
            Position = position;
            Type = type ?? ILiquorType.Untyped;
        }

        public ILiquorType Type { get; }

        public ModulePosition Position { get; }

        public override string ToString()
        {
            Indent += "  ";
            var result = new StringBuilder("{\n");

            for (int i = 0; i < Branches.Count; i++)
                result.Append(Branches[i].WriteToString(i));

            Indent = Indent[..^2];
            result.Append($"{Indent}}}");
            return result.ToString();
        }
    }
}
