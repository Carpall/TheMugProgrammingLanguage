using LLVMSharp.Interop;
using Mug.Grammar;
using Mug.Syntax;
using Mug.Syntax.AST;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using System.Buffers;

namespace Mug.Compilation
{
    public class CompilationInstance
    {
        public Diagnostic Diagnostic { get; } = new();
        public ImmutableArray<Source> Sources { get; }
        public string OutputFilename { get; }
        public List<DataType> Types { get; }
        
        public CompilationInstance(string outputFilename, ImmutableArray<Source> filepaths)
        {
            Sources = filepaths;
            OutputFilename = outputFilename;
            Types = new();
        }

        public CompilerResult<ImmutableArray<ImmutableArray<Token>>> GenerateTokens()
        {
            var lexer = new Lexer(this);
            var result = ImmutableArray.CreateBuilder<ImmutableArray<Token>>(Sources.Length);

            for (int i = 0; i < Sources.Length; i++)
            {
                var source = Sources[i];
                lexer.SetSource(source);
                result[i] = lexer.Tokenize();
            }

            return new(result.ToImmutable(), Diagnostic.GetException());
        }

        public CompilerResult<NamespaceNode> GenerateAST()
        {
            var tokens = GenerateTokens().Value;
            var parser = new Parser(this);
            var result = new NamespaceNode();

            for (int i = 0; i < tokens.Length; i++)
            {
                var tokensChunck = tokens[i];
                parser.SetTokens(tokensChunck);
                result.Members.AddRange(parser.Parse().Members);
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
        public void Throw(Grammar.Lexer lexer, int pos, string error)
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

        public void Report(Grammar.Lexer lexer, int position, string error)
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

        public static void Todo(string todo)
        {
            Throw($"TODO: {todo}");
        }
    }
}
