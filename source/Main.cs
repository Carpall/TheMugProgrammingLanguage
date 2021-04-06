using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using System;

try
{

#if DEBUG

    var test = @"

/*
  todo:
    [x] enum
    [ ] enum error / make enum automatic index generated (remove all enum error)
    [ ] split in files 'SymbolTable'
    [ ] look at the '// tofix' comments and 'throw new()'
    [ ] references [?]
*/

enum Some: u8 {
  ok: 0
  ok1: 1
}

func x(): Some!chr {
  return Some.ok1
}

func main() {
  x()
}
";

    var unit = new CompilationUnit("test.mug", test, true);

    // unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
    // Console.WriteLine(unit.GenerateAST().Dump());
    unit.Generate(true, true);

#else

    if (args.Length == 0)
        CompilationErrors.Throw("No arguments passed");

    var options = new CompilationFlags();

    options.SetArguments(args[1..]);

    options.InterpretAction(args[0]);

#endif
}
catch (CompilationException e)
{
    if (!e.IsGlobalError)
    {
        try
        {
            var errors = e.Lexer.DiagnosticBag.GetErrors();
            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];
                CompilationErrors.WriteSourceLineStyle(e.Lexer.ModuleName, error.Bad, error.LineAt(e.Lexer.Source), e.Lexer.Source, error.Message);
            }
        }
        catch
        {
            CompilationErrors.WriteFail(e.Lexer is not null ? e.Lexer.ModuleName : "", "Internal error: unable to print error message");
        }
    }
    else
        CompilationErrors.WriteFail(e.Lexer is not null ? e.Lexer.ModuleName : "", e.Message);
}