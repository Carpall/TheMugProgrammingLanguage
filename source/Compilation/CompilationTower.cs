using LLVMSharp.Interop;
using Mug.Models.Generator;
using Mug.Models.Generator.IR;
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
        public Lexer Lexer { get; set; }
        public Parser Parser { get; }
        public ASTSolver Solver { get; }
        public TypeInstaller TypeInstaller { get; }
        public MIRGenerator Generator { get; }
        public SymbolTable Symbols { get; }
        public List<DataType> Types { get; }
        public string OutputFilename { get; internal set; }
        public LLVMModuleRef LLVMModule { get; internal set; }

        public List<Token> TokenCollection => Lexer.TokenCollection;
        public NamespaceNode AST => Parser.Module;
        public MIR MIRModule => Generator.Module.Build();

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
