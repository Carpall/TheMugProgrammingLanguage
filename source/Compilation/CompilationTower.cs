using LLVMSharp.Interop;
using Mug.Models.Generator;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.AST;
using Mug.Symbols;
using Mug.TypeResolution;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mug.Compilation
{
    public class CompilationTower
    {
        public Diagnostic Diagnostic { get; } = new();
        public MugLexer Lexer { get; set; }
        public MugParser Parser { get; }
        public ASTSolver Solver { get; }
        public TypeInstaller TypeInstaller { get; }
        public MIRGenerator Generator { get; }
        public SymbolTable Symbols { get; }
        public List<MugType> Types { get; }
        public string ModuleName { get; internal set; }
        public LLVMModuleRef LLVMModule { get; internal set; }

        public List<Token> TokenCollection => Lexer.TokenCollection;
        public NamespaceNode AST => Parser.Module;

        public CompilationTower(string modulename)
        {
            ModuleName = modulename;
            Parser = new(this);
            Solver = new(this);
            TypeInstaller = new(this);
            Generator = new(this);
            Symbols = new(this);
            Types = new();
        }

        [DoesNotReturn()]
        public static void Throw(string error)
        {
            throw new CompilationException(error);
        }

        [DoesNotReturn()]
        public void Throw(MugLexer lexer, int pos, string error)
        {
            Throw(new ModulePosition(lexer, pos..(pos + 1)), error);
        }

        [DoesNotReturn()]
        public void Throw(Token token, string error)
        {
            Throw(token.Position, error);
        }

        [DoesNotReturn()]
        public void Throw(ModulePosition position, string error)
        {
            Diagnostic.Report(new(position, error));
            throw new CompilationException(error, Diagnostic);
        }

        public void Report(ModulePosition position, string error)
        {
            Diagnostic.Report(position, error);
        }

        public void Report(MugLexer lexer, int position, string error)
        {
            Report(new(lexer, position..(position + 1)), error);
        }

        public void CheckDiagnostic()
        {
            Diagnostic.CheckDiagnostic();
        }

        public void CheckDiagnostic(int errorsNumber)
        {
            if (Diagnostic.Count > errorsNumber)
                CheckDiagnostic();
        }

        internal static void Todo(string todo)
        {
            Throw($"TODO: {todo}");
        }
    }
}
