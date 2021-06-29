using Mug.Compilation;
using System;
using System.IO;

#if DEBUG

var pathHead = Path.GetFullPath(@"C:\Users\carpal\Desktop\mug\tests");
var unit = new CompilationUnit(true, "test.mir", pathHead, $"{pathHead}\\mainTest.mug");

/*PrettyPrinter.PrintAlerts(unit.GenerateAST(out var ast));

if (!unit.HasErrors())
    Console.WriteLine((ast as INode).Dump());*/

string ir;
CompilationException e;

Console.Write("(C | IR | LLVMIR): ");
var r = Console.ReadKey().KeyChar;
Console.WriteLine();

switch (r)
{
    case 'c': e = unit.GenerateC(out ir); break;
    default:
    case 'i': e = unit.GenerateIR(out var mir); ir = mir.ToString(); break;
    case 'l': e = unit.GenerateLLVMIR(out var llvmir); ir = llvmir.ToString(); break;
}

PrettyPrinter.PrintAlerts(e);

if (!unit.HasErrors())
    Console.WriteLine(ir);

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
