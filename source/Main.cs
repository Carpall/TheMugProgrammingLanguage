using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using System;

try
{

#if DEBUG

    // todo: fix enum errors, fix lexer tests
    // todo: add illegal recursion check in variants, check if variant contains multiple times the same type

    const string test = @"

func main(): i32 {
  y = x(y).do()
}

";
    
    var unit = new CompilationUnit("test.mug", test, true);

    // unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
    Console.WriteLine(unit.GenerateAST().Dump());
    // unit.Generate(true, true);

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
        var i = 0;
        var errors = e.Diagnostic.GetErrors();
        try
        {
            for (; i < errors.Count; i++)
            {
                var error = errors[i];
                CompilationErrors.WriteSourceLineStyle(error.Bad.Lexer.ModuleName, error.Bad.Position, error.LineAt(error.Bad.Lexer.Source), error.Bad.Lexer.Source, error.Message);
            }
        }
        catch
        {
            CompilationErrors.WriteFail(errors[i].Bad.Lexer.ModuleName, "Internal error: unable to print error message");
        }
    }
    else
        CompilationErrors.WriteFail("", e.Message);
}