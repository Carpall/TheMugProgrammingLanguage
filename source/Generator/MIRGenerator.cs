using Mug.Compilation;
using Mug.Generator.IR;
using System;

namespace Mug.Generator
{
    public class MIRGenerator
    {
        private CompilationTower Tower { get; }

        public MIRGenerator(CompilationTower tower)
        {
            Tower = tower;
        }

        public MIR Generator()
        {
            CompilationTower.Todo("fix mir generator");
            return new();
        }
    }
}
