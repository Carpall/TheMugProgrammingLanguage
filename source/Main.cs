using Nylon.Compilation;
using Nylon.Models.Parser;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Linq;

try
{

#if DEBUG

    // quick todo:
    //       - remove default values in for loop and other
    //       - add support for user defined operators (only for non-int based values)
    //       - add path checker
    //       - add pragmas' chekers
    //       - add defer statement
    //       - add doc comments for pub members
    //       - fix bugs with eof
    //       - add tuple types and 'new (,,)' for initialize them
    //       - fix tests
    //       - fix crash when error's position is on different lines
    //       - add implicit new operator with type inference 'new { }' and '[]', with context type
    //       - fix 'x *' 'x /'
    //       - fix '//' at the start of the line is not recognized as comment by the lexer
    //       - rename project in nylon
    //       - consider changing generic parameters from '<>' to '[]'
    //       - add attributes to NIR._allocations 'const', 'hiddenbuf'
    //       - add check for uninitialized memory
    //       - add warnings
    
    var unit = new CompilationUnit("test.nir", @"../../../../tests/main_test.nyl");
    
    // unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
    // Console.WriteLine((unit.GenerateAST() as INode).Dump());
    // Console.WriteLine((unit.GenerateTAST() as INode).Dump());
    Console.WriteLine(unit.GenerateNIR().Dump());

#else

    var options = new CompilationFlags();

    if (args.Length == 0)
        CompilationFlags.PrintUsageAndHelp();
    else
    {
        options.SetArguments(args[1..]);

        options.InterpretAction(args[0]);
    }

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
                PrettyPrinter.WriteSourceLineStyle(
                    error.Bad.Lexer.ModuleName,
                    error.Bad.Position,
                    error.Bad.LineAt(),
                    error.Bad.Lexer.Source,
                    error.Message);
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