using Mug.Compilation;
using Mug.Models.Parser;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Linq;
using Mug.Models.Generator.IR;

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
//       - add warnings
//       - add calls to supported hidden allocation expressions
//       - add implicit type for parameters 'function(a, b: i32)'
//       - add optional parameter
//       - add varargs
//       - change is into a new node, not boolean

var unit = new CompilationUnit("test.mir", @"../../../../tests/main_test.mug");

// unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
// Console.WriteLine((unit.GenerateAST() as INode).Dump());
// Console.WriteLine((unit.GenerateTAST() as INode).Dump());

PrettyPrinter.PrintAlerts(unit.GenerateMIR(out var ir));

if (!unit.HasErrors())
    Console.WriteLine(ir.Dump());

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
