using Mug.AstGeneration.IR.Values;
using Mug.AstGeneration.IR.Values.Instructions;
using Mug.Compilation;
using Mug.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR
{
    public class LiquorIR
    {
        public List<LiquorComptimeVariable> Declarations { get; } = new();

        public override string ToString()
        {
            return new LiquorIRWriter(this).WriteToString();
        }

        internal void EmitGlobalDeclaration(string name, ILiquorValue body, ModulePosition position, INode type)
        {
            Declarations.Add(new(name, body, position, type));
        }
    }
}
