using LLVMSharp.Interop;
using Nylon.Models.Generator;
using Nylon.Models.Generator.IR;
using Nylon.Models.Lexer;
using Nylon.Models.Parser;
using Nylon.Models.Parser.AST;
using Nylon.Symbols;
using Nylon.TypeResolution;
using Nylon.TypeSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nylon.Compilation
{
    public class CompilationTower
    {
        public Diagnostic Diagnostic { get; } = new();
        public Lexer Lexer { get; set; }
        public Parser Parser { get; }
        public ASTSolver Solver { get; }
        public TypeInstaller TypeInstaller { get; }
        public NIRGenerator Generator { get; }
        public SymbolTable Symbols { get; }
        public List<DataType> Types { get; }
        public string OutputFilename { get; internal set; }
        public LLVMModuleRef LLVMModule { get; internal set; }

        public List<Token> TokenCollection => Lexer.TokenCollection;
        public NamespaceNode AST => Parser.Module;
        public NIR NIRModule => Generator.Module.Build();

        public CompilationTower(string outputFilename)
        {
            OutputFilename = outputFilename;
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

        public bool HasErrors()
        {
            return Diagnostic.Count > 0;
        }

        [DoesNotReturn()]
        public void Throw(Lexer lexer, int pos, string error)
        {
            Throw(new ModulePosition(lexer, pos..(pos + 1)), error);
        }

        [DoesNotReturn()]
        public void Throw(Token token, string error)
        {
            Throw(token.Position, error);
        }

        public void Warn(ModulePosition position, string message)
        {
            Diagnostic.Warn(position, message);
        }

        [DoesNotReturn()]
        public void Throw(ModulePosition position, string error)
        {
            Diagnostic.Report(new(CompilationAlertKind.Error, position, error));
            throw new CompilationException(Diagnostic);
        }

        public void Report(ModulePosition position, string error)
        {
            Diagnostic.Report(position, error);
        }

        public void Report(Lexer lexer, int position, string error)
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
