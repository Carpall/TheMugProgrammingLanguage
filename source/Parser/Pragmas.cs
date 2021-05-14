using Nylon.Compilation;
using Nylon.Models.Lexer;
using System;
using System.Collections.Generic;

namespace Nylon.Models.Parser
{
  public class Pragmas
    {
        private readonly Dictionary<string, Token> _table = new()
        {
            ["inline"    ] = Token.NewInfo(TokenKind.ConstantBoolean, "false"),
            ["test"      ] = Token.NewInfo(TokenKind.ConstantBoolean, "false"),
            ["header"    ] = Token.NewInfo(TokenKind.ConstantString, ""      ),
            ["dynlib"    ] = Token.NewInfo(TokenKind.ConstantString, ""      ),
            ["export"    ] = Token.NewInfo(TokenKind.ConstantString, ""      ),
            ["extern"    ] = Token.NewInfo(TokenKind.ConstantString, ""      ),
            ["code"      ] = Token.NewInfo(TokenKind.ConstantString, ""      ),
            ["clang_args"] = Token.NewInfo(TokenKind.ConstantString, ""      ),
            ["ext"       ] = Token.NewInfo(TokenKind.ConstantString, ""      ),
        };

        public string GetPragma(string pragma)
        {
            return _table[pragma].Value;
        }

        private void SetWithCheck(string pragma, string symbol)
        {
            if (_table[pragma].Value == "")
                _table[pragma] = Token.NewInfo(TokenKind.ConstantString, symbol);
        }

        public void SetName(string symbol)
        {
            SetWithCheck("export", symbol);
        }

        public void SetExtern(string symbol)
        {
            SetWithCheck("extern", symbol);
        }

        public void SetPragma(string pragma, Token value, CompilationTower tower, ModulePosition position)
        {
            if (!_table.ContainsKey(pragma))
                tower.Report(position, "Unknown pragma");

            _table[pragma] = value;
        }
    }
}
