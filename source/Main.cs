using Mug.Compilation;
using System;
using System.Collections.Immutable;

#if DEBUG

const string path = @"C:\Users\carpal\Desktop\mug\tests\mainTest.mug";

var compiler = new CompilationInstance("test", ImmutableArray.Create(Source.ReadFromPath(path)));

/*PrettyPrinter.PrintAlerts(unit.GenerateAST(out var ast));

if (!unit.HasErrors())
    Console.WriteLine((ast as INode).Dump());*/

string output;
CompilationException e;

Console.Write("(C | IR | LLVMIR): ");
var r = Console.ReadKey().KeyChar;
Console.WriteLine();

switch (r)
{
    case 'a': compiler.GenerateAST(); break;
}

PrettyPrinter.PrintAlerts(e);

if (!compiler.HasErrors())
    Console.WriteLine(output);

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
