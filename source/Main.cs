using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using System;

try
{

#if DEBUG

    // todo: remove getmainfile in compilationflags
    //  fix as operators

    var test = @"

func add<T>(a: T, b: T): T { return a + b }

func a<T1>() { add<T1>(1, 2) }

func main() {
  a<i32>()
}
";

    var unit = new CompilationUnit("test.mug", test, true);

    // unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
    // Console.WriteLine(unit.GenerateAST().Dump());
    unit.Generate(true, true);

#else
    // args = new[] { "build", "C:/Users/carpal/Desktop/mug/tests/workspace/dot/main.mug" };
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
        try
        {
            var errors = e.Lexer.DiagnosticBag.GetErrors();
            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];
                CompilationErrors.WriteSourceLineStyle(error.Bad.Lexer.ModuleName, error.Bad.Position, error.LineAt(e.Lexer.Source), e.Lexer.Source, error.Message);
            }
        }
        catch
        {
            CompilationErrors.WriteFail(e.Lexer is not null ? e.Lexer.ModuleName : "", "Internal error: unable to print error message");
        }
    }
    else
        CompilationErrors.WriteFail(e.Lexer is not null ? e.Lexer.ModuleName : "", e.Message);
}