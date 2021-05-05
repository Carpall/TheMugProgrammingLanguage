using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Compilation
{
    public class ZapComponent
    {
        public CompilationTower Tower { get; }

        public ZapComponent(CompilationTower tower)
        {
            Tower = tower;
        }
    }
}
