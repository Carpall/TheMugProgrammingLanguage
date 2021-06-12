using Mug.Compilation;
using Mug.Tokenizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Symbols
{
    public interface ISymbol
    {
        public ModulePosition Position { get; }

        public TokenKind Modifier { get; }
    }
}
