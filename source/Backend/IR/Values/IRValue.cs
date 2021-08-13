using Mug.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Backend.IR.Values
{
    public interface IRValue
    {
        public ModulePosition Position { get; }
    }
}
