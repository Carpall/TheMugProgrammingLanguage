using Nylon.Compilation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nylon.Symbols
{
    public class SymbolTable : CompilerComponent
    {
        private Dictionary<string, ISymbol> _symbols = new();

        public SymbolTable(CompilationTower tower) : base(tower)
        {
        }

        public T GetSymbol<T>(string name, ModulePosition position, string memberKind) where T : ISymbol
        {
            if (!_symbols.TryGetValue(name, out var symbol))
                Tower.Report(position, $"'{name}' is not declared");
            else if (symbol is not T)
                Tower.Report(position, $"'{name}' is not a {memberKind}");
            else
                return (T)symbol;

            return default;
        }

        public void SetSymbol(string name, ISymbol symbol)
        {
            if (!_symbols.TryAdd(name, symbol))
                Tower.Report(symbol.Position, $"'{name}' is already declared");
        }

        internal Dictionary<string, ISymbol> GetCache()
        {
            return _symbols;
        }

        public string Dump()
        {
            var result = new StringBuilder();

            foreach (var symbol in _symbols)
                result.AppendLine(symbol.Value.Dump(true));

            return result.ToString();
        }
    }
}