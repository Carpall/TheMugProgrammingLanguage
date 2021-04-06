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
    [x] enum error / make enum automatic index generated (remove all enum error)
    [x] auto generated enumeration in enumerated 
    [x] split in files 'SymbolTable'
    [ ] look at the '// tofix' comments and 'throw new()'
*/

type A { a: chr }

func `as`(value: A): chr {
  return value.a
}

func `as`(value: A): chr {
  return value.a
}

func main(): i32 {
  return (new A {} as chr) as i32
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
                CompilationErrors.WriteSourceLineStyle(error.Bad.Lexer.ModuleName, error.Bad.Position, error.LineAt(e.Lexer.Source), e.Lexer.Source, error.Message);
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