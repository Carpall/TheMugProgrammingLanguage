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

    const string test = @"

func malloc(size: i64): unknown
func free(alloc: unknown)

func heap<T>(value: T): *T {
	const allocation = malloc(size<T>()) as *T
	*allocation = value
	return allocation
}

func (self: *T) free<T>() {
	free(self as unknown)
}

// type Lexer { src: str }

func main() {
	var x = heap<i32>(10)
	x.free<i32>()
}

";
    
    var unit = new CompilationUnit("test.mug", test, true);

    // unit.IRGenerator.Parser.Lexer.Tokenize().ForEach(token => Console.WriteLine(token));
    // Console.WriteLine(unit.GenerateAST().Dump());
    unit.Generate(true, true);

#else

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
        var i = 0;
        var errors = e.Diagnostic.GetErrors();
        try
        {
            for (; i < errors.Count; i++)
            {
                var error = errors[i];
                CompilationErrors.WriteSourceLineStyle(error.Bad.Lexer.ModuleName, error.Bad.Position, error.LineAt(error.Bad.Lexer.Source), error.Bad.Lexer.Source, error.Message);
            }
        }
        catch
        {
            CompilationErrors.WriteFail(errors[i].Bad.Lexer.ModuleName, "Internal error: unable to print error message");
        }
    }
    else
        CompilationErrors.WriteFail("", e.Message);
}