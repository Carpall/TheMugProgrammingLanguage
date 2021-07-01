using Mug.Compilation;
using System;
using System.Collections.Immutable;
using System.IO;

#if DEBUG

var path = @"../tests/mainTest.mug";

var compiler = new CompilationInstance("test", ImmutableArray.Create(Source.ReadFromPath(path)));

/*PrettyPrinter.PrintAlerts(unit.GenerateAST(out var ast));

if (!unit.HasErrors())
    Console.WriteLine((ast as INode).Dump());*/
    
Console.Write("( ast ): ");
var r = Console.ReadKey().KeyChar;
Console.WriteLine();

printResult(
    r switch {
        'a' => compiler.GenerateAST(),
        'g' => compiler.CheckAST(),
        _ => new()
    }
);

static void printResult<T>(CompilerResult<T> result)
{
    if (result.IsGood())
        Console.WriteLine(result.Value.ToString());
    else
        PrettyPrinter.PrintAlerts(result.Exception);
}

#else

var options = new CompilationFlags();

if (args.Length == 0)
    CompilationFlags.PrintUsageAndHelp();
else
{
    options.SetArguments(args[1..]);

    try { options.InterpretAction(args[0]); } catch (CompilationException e) { PrettyPrinter.PrintAlerts(e); }
}

#endif
