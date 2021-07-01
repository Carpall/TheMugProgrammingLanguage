using LLVMSharp.Interop;
using Mug.Grammar;
using Mug.Syntax;
using Mug.Syntax.AST;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using System.Buffers;
using Mug.AstGeneration;
using Mug.AstGeneration.IR;

namespace Mug.Compilation
{
    public class CompilationInstance
    {
        public Diagnostic Diagnostic { get; } = new();
        public ImmutableArray<Source> Sources { get; }
        public string OutputFilename { get; }
        
        public CompilationInstance(string outputFilename, ImmutableArray<Source> filepaths)
        {
            Sources = filepaths;
            OutputFilename = outputFilename;
        }

        public CompilerResult<LiquorIR> GenerateIR()
        {
            var ast = GenerateAST().Value;
            var evaluator = new AstGenerator(this);
            evaluator.SetAST(ast);

            return new(evaluator.Generate(), Diagnostic.GetException());
        }

        public CompilerResult<ImmutableArray<ImmutableArray<Token>>> GenerateTokens()
        {
            var lexer = new Lexer(this);
            var result = ImmutableArray.CreateBuilder<ImmutableArray<Token>>(Sources.Length);

            foreach (var source in Sources)
            {
                lexer.SetSource(source);
                try { result.Add(lexer.Tokenize()); }
                catch (CompilationException) { break; }
            }

            return new(result.ToImmutable(), Diagnostic.GetException());
        }

        public CompilerResult<NamespaceNode> GenerateAST()
        {
            var tokens = GenerateTokens().Value;
            var parser = new Parser(this);
            var result = new NamespaceNode();

            foreach (var subtokens in tokens)
            {
                parser.SetTokens(subtokens);
                try { result.Members.AddRange(parser.Parse().Members); }
                catch (CompilationException) { break; }
            }

            return new(result, Diagnostic.GetException());
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
        public void Throw(Source source, int pos, string error)
        {
            Throw(new ModulePosition(source, pos..(pos + 1)), error);
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

        public void Report(Source source, int position, string error)
        {
            Report(new(source, position..(position + 1)), error);
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

        public static void Todo(string todo)
        {
            Throw($"TODO: {todo}");
        }
    }
}
