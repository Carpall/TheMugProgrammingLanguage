using Mug.Compilation;
using Mug.Models.Parser;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

try
{

#if DEBUG

    // todo: - check type recursion
    //       - add function overloading
    //       - add default values
    //       - add support for user defined operators (only for non-int based values)
    //       - merge all files passed
    //       - add calls

    const string test = @"

func add(a: u8, b: u8): u8 { a + b }

func main() {
  const add = add(1, 2)
}

";

    var unit = new CompilationUnit(@"test.mug", test);

    // unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
    // Console.WriteLine((unit.GenerateAST() as INode).Dump());
    // Console.WriteLine((unit.GenerateTAST() as INode).Dump());
    Console.WriteLine(unit.GenerateMIR().Dump());

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
                PrettyPrinter.WriteSourceLineStyle(error.Bad.Lexer.ModuleName, error.Bad.Position, error.Bad.LineAt(), error.Bad.Lexer.Source, error.Message);
            }
        }
        catch
        {
            PrettyPrinter.WriteFail(errors[i].Bad.Lexer.ModuleName, "Internal error: unable to print error message");
        }
    }
    else
        PrettyPrinter.WriteFail("", e.Message);
}