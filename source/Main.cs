using Mug.Compilation;
using System;
using System.Collections.Immutable;
using System.IO;

#if DEBUG

// const string path = @"../tests/mainTest.mug";
const string path = @"C:\Users\carpal\Desktop\mug\tests\mainTest.mug";

var compiler = new CompilationInstance("test", ImmutableArray.Create(Source.ReadFromPath(path)));

/*PrettyPrinter.PrintAlerts(unit.GenerateAST(out var ast));

if (!unit.HasErrors())
    Console.WriteLine((ast as INode).Dump());*/
    
Console.Write("( [1] ast | [2] ast check ): ");
var r = Console.ReadKey().KeyChar;
Console.WriteLine();

printResult(
    r switch {
        '1' => compiler.GenerateAST(),
        '2' => compiler.GenerateASTAndCheck(),
        _ => null
    }
);

static void printResult(dynamic result)
{
    if (result is null)
        return;

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
