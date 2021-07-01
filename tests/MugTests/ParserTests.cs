using Mug.Compilation;
using Mug.Grammar;
using Mug.Syntax;
using Mug.Syntax.AST;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Mug.Tests
{
    public class ParserTests
    {
        // Well constructed code strings
        private readonly string[] _validTests =
        {
            @"pub const main = fn { }",
            @"
pub const ImmutableHeapArray = fn(static T: type):type {
  struct {
    cache: *T,
    len: usize,
    
    static const Self = self!()
    pub static const create = fn(): Self {
      new {
        cache: none!()
      }
    }

    pub const len = fn(): usize { self.len }
  }
}",
            @"const _=new module.submodule.subsbubmodule.Structure { }",
            @"const _=new module.GenericStructure(T) { }",
            @"const _=new { a: 1, }",
            @"const _=new { a: 1 }",
            @"const _=10*2+2",
            @"const _= 10 * (1 + 2)",
            @"const _=new { a: 1 }",
            @"const _='a'.func()",
            @"const _=0.func()",
            @"const _=0.1",
            @"const _=0.1f",
            @"const _=1f",
            @"const _='a'.field.subfield.func()",
            @"const _='a'.field.subfield.func!()",
            @"const _=a[0]",
            @"const _=a[b[c[a.l.l]]].a",
            @"const _=a[b[c[a.l.l]]].a()",
            @"const _=a[b[c[a.l.l]]].a!()",
            @"const _=a[1]()",
            @"const _=a[1](1, 2)",
            @"const _=a[1].a(1, 2)",
            @"const _=a[1]()",
            @"const _ = { }",
            @"const _=1 * 2 + 3 * 4 / (1 - 2 * (*ptr)) + -1",
            @"const _=a[a].a",
            @"const _=a[a][a][a].a",
            @"const _=a[a][a][a].a()",
            @"const _=a.a().a().a().a",
            @"const _=!a",
            @"
const _= {
  f()
  a
}",
            @"let mut x = 1",
            @"
const Array = struct {
  pub len: i32,
  pub len1: u8
  pub len2: T.T
  priv len3: (*T),

  static const Self = self!()

  pub static const create = fn(): Self {
    new { len: 1, }
  }
}

[test]
const test = fn {
  let mut x = Array.create()
}
",
            @"const _= { return }",
            @"const _= { return 1 }",
            @"const _ = (fn(a, b: i32): i32 { a + b })(1, 2)",
            @"const _ = (struct { }).field",
            @"const _ = (enum { }).member",
            @"const _ = enum u8 { }",
            @"const _ = enum module.submodule.submodule.Struct { a, b, c }",
            @"const _ = enum { a, b: 1, c: 2, }",
            @"const _ = enum { a, }",
            @"const _= {
              return
            }",
            @"const _= {
              return // return void
              x // this won't be returned, it's just an expression to evaluate as statement
            }",
            @"",
            @"const _ = { for x in y { } }"
        };

        private readonly string[] _invalidTests =
        {
            @"const _=fn",
            @"const _=a[a][a][",
            @"const _=a.",
            @"const _=a-",
            @"const _={",
            @"const _=}",
            @"const _=(",
            @"const _=)",
            @"static static const x = 1",
            @"const _=",
            @"const _={.}",
            @"const _=@",
            @"return",
            @"o",
            @"1",
            @"var",
            @"let",
            @"const _=struct fn .",
            @"const _=()",
            @"const _=x/",
            @"const _=/x",
            @"const _= { return /x }",
            @"const _= { return x/ }",
            @"const _=(((())))",
            @"const _=!",
            @"const _=a!",
            @"const _ = fn(a, b: i32): i32 { a + b }(1, 2)",
            @"const _ = struct { }.field",
            @"const _ = enum { }.member",
            @"const _ = enum T { a b c }",
            @"const _ = enum { a, b: 1 c }",
            @"const _ = enum { a, , }",
        };

        [Test]
        public void RunningAllTests()
        {
            RunValidTests();
            RunInvalidTests();
        }

        private void RunInvalidTests()
        {
            Console.WriteLine("RUNNING INVALID TESTS ({0})", _invalidTests.Length);
            for (int i = 0; i < _invalidTests.Length; i++)
            {
                var test = _invalidTests[i];
                var testname = $"invalidtest{i}";

                var instance = NewCompilationInstance(test, testname);
                var result = instance.GenerateAST();
                if (result.IsGood())
                {
                    Console.WriteLine("{0} is not invalid", testname);
                    Console.WriteLine("test:\n{0}", test);
                    Assert.Fail();
                }
            }
        }

        private void RunValidTests()
        {
            Console.WriteLine("RUNNING VALID TESTS ({0})", _validTests.Length);
            for (int i = 0; i < _validTests.Length; i++)
            {
                var test = _validTests[i];
                var testname = $"test{i}";

                var instance = NewCompilationInstance(test, testname);
                var result = instance.GenerateAST();
                if (!result.IsGood())
                {
                    Error(testname, result, test, null, null, null);
                    continue;
                }

                var ast = result.Value;
                var astJson = (ast as INode).Dump();
                var rebuilt = ast.ToString();
                testname = $"Rebuilt-{testname}";
                instance = NewCompilationInstance(rebuilt, testname);

                result = instance.GenerateAST();
                var newAstJson = (result.Value as INode).Dump();

                if (!result.IsGood())
                {
                    Error(testname, result, test, astJson, rebuilt, newAstJson);
                    continue;
                }

                if (!astJson.Equals(newAstJson))
                    Fail(test, testname, astJson, rebuilt, newAstJson);
            }
        }

        private static void Fail(string test, string testname, string astJson, string rebuilt, string newAstJson)
        {
            Console.WriteLine("Failed running {0}", testname);

            Console.WriteLine("Source:");
            Console.WriteLine(test);
            Console.WriteLine("Rebuilt:");
            Console.WriteLine(rebuilt);
            Console.WriteLine("Ast:");
            Console.WriteLine(astJson);
            Console.Write("\n\n\n\n------------\n\n\n\n");
            Console.WriteLine(newAstJson);
            Assert.Fail();
        }

        private static CompilationInstance NewCompilationInstance(string test, string testname)
        {
            return new CompilationInstance(testname, ImmutableArray.Create(new Source(testname, test)));
        }

        private static void Error(string testname, CompilerResult<NamespaceNode> result, string test, string astJson, string rebuilt, string newAstJson)
        {
            Console.WriteLine("Found errors on {0}", testname);
            foreach (var alert in result.Exception.Diagnostic.GetAlerts())
                Console.WriteLine("{0}, at {1}: {2}", alert.Kind, alert.Bad, alert.Message);
            Console.WriteLine("__________________________________________________________");
            Fail(test, testname, astJson, rebuilt, newAstJson);
        }
    }
}
