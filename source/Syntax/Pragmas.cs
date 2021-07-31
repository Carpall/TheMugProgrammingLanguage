using Mug.Compilation;
using Mug.Grammar;
using Mug.Syntax.AST;
using System;
using System.Collections.Generic;

namespace Mug.Syntax
{
    public class Pragmas
    {
        private readonly Dictionary<string, INode> _table = new()
        {
            ["inline"    ] = null,
            ["test"      ] = null,
            ["packed"    ] = null,
            ["import"    ] = null,
            ["export"    ] = null
        };

        public int Count => _table.Count;

        public void Clear()
        {
            _table.Clear();
        }

        public INode GetPragma(string pragma)
        {
            return _table[pragma];
        }

        internal void SetPragma(string pragma, INode value, CompilationInstance tower, ModulePosition position)
        {
            if (!_table.ContainsKey(pragma))
                tower.Report(position, "Unknown pragma");
            else if (_table[pragma] is not null)
                tower.Report(position, "Pragma already assigned");
            else
                _table[pragma] = value;
        }
    }
}
