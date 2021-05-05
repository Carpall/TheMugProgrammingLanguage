using Zap.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Symbols
{
    public interface ISymbol
    {
        public ModulePosition Position { get; }

        public abstract string Dump(bool dumpmodel);
    }
}
