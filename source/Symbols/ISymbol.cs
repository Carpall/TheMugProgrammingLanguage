using Mug.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Symbols
{
    public interface ISymbol
    {
        public string Description { get; }
        public ModulePosition Position { get; }
    }
}
