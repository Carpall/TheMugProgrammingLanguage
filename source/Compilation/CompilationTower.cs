using LLVMSharp.Interop;
using Mug.Generator.TargetGenerators;
using Mug.Generator.TargetGenerators.LLVM;
using Mug.Generator;
using Mug.Generator.IR;
using Mug.Lexer;
using Mug.Parser;
using Mug.Parser.AST;
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
        public string OutputFilename { get; }
        public Lexer.Lexer Lexer { get; set; }
        public Parser.Parser Parser { get; }
        public ASTSolver Solver { get; }
        public TypeInstaller TypeInstaller { get; }
        public MIRGenerator Generator { get; }
        public TargetGenerator TargetGenerator { get; set; }
        public SymbolTable Symbols { get; }
        public List<DataType> Types { get; }
        public CompilationFlags Flags { get; }
        public List<Token> TokenCollection => Lexer.TokenCollection;
        public NamespaceNode AST => Parser.Module;
        public NamespaceNode TAST { get; internal set; }
        public MIR MIRModule { get; internal set; }
        public LLVMModuleRef LLVMModule { get; internal set; }
        public string CModule { get; internal set; }

        public CompilationTower(string outputFilename, CompilationFlags flags = null)
        {
            OutputFilename = outputFilename;
            Parser = new(this);
            Solver = new(this);
            TypeInstaller = new(this);
            Generator = new(this);
            Symbols = new(this);
            Types = new();
            Flags = flags;
        }

        public void SetGenerator(TargetGenerator generator)
        {
            TargetGenerator = generator;
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
        public void Throw(Lexer.Lexer lexer, int pos, string error)
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

        public void Report(Lexer.Lexer lexer, int position, string error)
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

        internal bool IsExpectingEntryPoint()
        {
            return Flags is not null && Flags.GetFlag<CompilationTarget>("target") is CompilationTarget.EXE;
        }
    }
}
