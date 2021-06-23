using Mug.Compilation;
using Mug.Tokenizer;
using Mug.Parser.AST.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mug.Symbols
{
    public class SymbolTable : CompilerComponent
    {
        public Dictionary<string, SymbolTable> ImportedModules { get; } = new();
        public List<string[]> References { get; } = new();
        private readonly Dictionary<string, ISymbol> _symbols = new();

        public SymbolTable(CompilationTower tower) : base(tower)
        {
        }

        public T GetSymbol<T>(string name, ModulePosition position, string memberKind) where T : ISymbol
        {
            var symbolsSource = _symbols;
            var index = 0;
            string moduleName = null;
            ISymbol symbol;

            while (!symbolsSource.TryGetValue(name, out symbol))
            {
                if (index >= ImportedModules.Values.Count)
                {
                    Tower.Report(position, $"'{name}' is not declared");
                    return default;
                }

                moduleName = ImportedModules.Keys.ElementAt(index++);
                symbolsSource = ImportedModules[moduleName]._symbols;
            }

            if (symbol.Modifier is not TokenKind.KeyPub && index > 0)
                Tower.Report(position, $"'{name}' is a private member of module '{moduleName}'");

            if (symbol is not T)
            {
                Tower.Report(position, $"'{name}' is not a {memberKind}");
                return default;
            }

            return (T)symbol;
        }

        public void SetSymbol(string name, ISymbol symbol)
        {
            var previouslyDeclared = GetPreviouslyDeclaredOrDeclare(name, symbol);
            if (IsPreviouslyDeclared(previouslyDeclared))
                ReportRedeclaration($"'{name}' is already declared", symbol.Position, previouslyDeclared.Position);
        }

        public bool SymbolIsDeclared(string symbol)
        {
            return _symbols.ContainsKey(symbol);
        }

        internal void ReportRedeclaration(
            string message,
            ModulePosition position,
            ModulePosition previouslyDeclaredPosition = new())
        {
            Tower.Report(position, message);
            if (previouslyDeclaredPosition.Position.End.Value == 0)
                Tower.Warn(previouslyDeclaredPosition, $"Previously declared here");
        }

        private static bool IsPreviouslyDeclared(ISymbol previouslyDeclared)
        {
            return previouslyDeclared is not null;
        }

        private ISymbol GetPreviouslyDeclaredOrDeclare(string name, ISymbol symbol)
        {
            foreach (var declaredSymbol in _symbols)
                if (declaredSymbol.Key == name)
                    return declaredSymbol.Value;

            _symbols.Add(name, symbol);
            return null;
        }

        internal Dictionary<string, ISymbol> GetCache()
        {
            return _symbols;
        }

        public string Dump()
        {
            var result = new StringBuilder();

            foreach (var symbol in _symbols)
                result.AppendLine(symbol.Value.ToString());

            return result.ToString();
        }
    }
}