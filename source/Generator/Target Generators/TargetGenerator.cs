using Mug.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.TargetGenerators
{
    public abstract class TargetGenerator : CompilerComponent
    {
        public TargetGenerator(CompilationTower tower) : base(tower)
        {
        }

        public abstract object Lower();
    }
}
