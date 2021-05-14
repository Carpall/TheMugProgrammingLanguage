using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.Compilation
{
    public class CompilerComponent
    {
        public CompilationTower Tower { get; }

        public CompilerComponent(CompilationTower tower)
        {
            Tower = tower;
        }
    }
}
