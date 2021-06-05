using Mug.Compilation;
using Mug.Parser;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Linq;
using Mug.Generator.IR;

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
//       - fix '//' at the start of the line is not recognized as comment by the lexer
//       - consider changing generic parameters from '<>' to '[]'
//       - add check for uninitialized memory
//       - add implicit type for parameters 'function(a, b: i32)'
//       - add optional parameters
//       - add varargs
//       - add arrays
//       - change is into a new node, not boolean
//       - add a warn for user defined types named like primitives
//       - reimplement generics in function calls in the parser, temporary disabled due to other ideas about their syntax design
//       - add compiler symbols and use them to get target int size
//       - fix 'return /x'
//       - add function prototypes
//       - fix folding '2 * 3 * 3'
//       - fix bool folding
//       - add condition folding
//       - add import global statement
//       - add a error for functions with more than 10 parameters
//       - design in compilation flags a way not to update the help message

var unit = new CompilationUnit("test.mir", null, @"../../../../tests/main_test.mug");

/*PrettyPrinter.PrintAlerts(unit.GenerateAST(out var ast));

if (!unit.HasErrors())
    Console.WriteLine((ast as INode).Dump());*/

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
