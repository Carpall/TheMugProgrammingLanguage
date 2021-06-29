using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Compilation
{
    public class CompilerComponent
    {
        public CompilationInstance Tower { get; }

        public CompilerComponent(CompilationInstance tower)
        {
            Tower = tower;
        }
    }
}
