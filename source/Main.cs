using Zap.Compilation;
using Zap.Models.Parser;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Linq;

try
{

#if DEBUG

    // quick todo:
    //       - check type recursion
    //       - add function overloading
    //       - remove default values in for loop and other
    //       - add support for user defined operators (only for non-int based values)
    //       - add calls
    //       - add path checker
    //       - make all a expression as terms allowing return value in hidden buffer with `break value`
    //       - add pragmas' chekers
    //       - add deref statement
    //       - add option type '?type'
    //       - add doc comments for pub members
    //       - add implicit true values in pragmas
    //       - fix bugs with eof
    //       - add tuple types and 'new (,,)' for initialize them
    //       - fix tests
    //       - fix positions
    //       - fix crash when error's position is on different lines
    
    var unit = new CompilationUnit("test.mir", @"../../../../tests/main_test.zap");
    
    // unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
    // Console.WriteLine((unit.GenerateAST() as INode).Dump());
    // Console.WriteLine((unit.GenerateTAST() as INode).Dump());
    Console.WriteLine(unit.GenerateZAR().Dump());

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