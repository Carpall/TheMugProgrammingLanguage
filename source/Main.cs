using Mug.Compilation;
using Mug.Parser;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Linq;
using Mug.Generator.IR;
using System.Collections.Generic;

#if DEBUG

var pathHead = Path.GetFullPath(@"../../../../tests");
var unit = new CompilationUnit(true, "test.mir", pathHead, $"{pathHead}/mainTest.mug");

/*PrettyPrinter.PrintAlerts(unit.GenerateAST(out var ast));

if (!unit.HasErrors())
    Console.WriteLine((ast as INode).Dump());*/

// var e = unit.GenerateC(out var ir);

var e = unit.GenerateIR(out var ir);

// var e = unit.GenerateLLVMIR(out var ir);

PrettyPrinter.PrintAlerts(e);

if (!unit.HasErrors() /*&& ir is not null*/)
    Console.WriteLine(ir.ToString());

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
