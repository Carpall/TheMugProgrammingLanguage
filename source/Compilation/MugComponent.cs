using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Compilation
{
    public class MugComponent
    {
        public CompilationTower Tower { get; }

        public MugComponent(CompilationTower tower)
        {
            Tower = tower;
        }
    }
}
