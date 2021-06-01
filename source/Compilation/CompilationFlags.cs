using LLVMSharp.Interop;
using Mug.Generator.IR;
using Mug.Parser;
using Mug.Parser.AST;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mug.Compilation
{
    public enum CompilationMode
    {
        Release = 3,
        Debug = 0
    }

    public enum CompilationTarget
    {
        EXE,
        Library, // not available yet
        BC,
        ASM,
        AST,
        LL,
        TAST,
        MIR,
        MIRJSON
    }

    public enum CompilationMeans
    {
        LLVM,
        C
    }

    public class CompilationFlags
    {
        private static readonly string[] _targets = { "exe", "lib", "bc", "asm", "ast", "ll", "tast", "mir", "mir-json" };

        private const string USAGE = "\nUSAGE: mug <action> <file> <options>\n";
        private static readonly string HELP = @$"
Compilation Actions:
  - build: to compile a program, with the following default options: {{target: exe, mode: debug, output: <file>.exe}}
  - run: build and run
  - check: prints errors in a source, without generating anything
  - help: show this list or describes a compilation flag when one argument is passed

Compilation Flags:
  - src: source file to compile (one at time)
  - mode: the compilation mode: {{release: fast and small exe, debug: faster compilation, slower exe, allows to use llvmdbg}}
  - target: file format to generate: {{{string.Join(", ", _targets)}}}
  - output: output file name
  - args: arguments to pass to the compiled program

How To Use:
  - compilation action: it's a command to give to the compiler, only one compilation action for call
  - compilation flag: it's a directive to give to the compilatio action, each compilation flag must be preceded by *
";
        private const string SRC_HELP = @"
USAGE: mug <action> <options> *src <file>

HELP: uses the next argument as source file to compile
";
        private const string MODE_HELP = @"
USAGE: mug <action> <file> <options> *mode (debug | release)

HELP: uses the next argument as compilation mode:
  - debug: for a faster compilation, allows debugging with llvmdbg
  - release: for a faster runtime execution, supports code optiminzation
";
        private static readonly string TARGET_HELP = @$"
USAGE: mug build <file> <options> *target ( {string.Join(" | ", _targets)} )

HELP: uses the next argument as compilation target:
  - exe: executable with platform specific extension
  - lib: dynamic link library
  - bc: llvm bitcode
  - asm: clang assembly
  - ast: abstract syntax tree
  - tast: ast with types resolved
  - ll: llvm bytecode
  - mir: internal ir
  - mir-json: internal ir in json format
";
        private const string DEC_HELP = @"
USAGE: mug <action> <file> <options> *dec symbol

HELP: uses the next argument as symbol to declare before the compilation:
";
        private const string OUTPUT_HELP = @"
USAGE: mug <action> <file> <options> *output <name>

HELP: uses the next argument as output file name. The extension is not required
";
        private const string ARGS_HELP = @"
USAGE: mug run <file> <options> *args ""<arg1> <arg2> ...""

HELP: uses the next argument as arguments to pass to the compiled program, available only when compilation action is 'run'
";

        private string[] _arguments = null;
        private int _argumentSelector = 0;
        private readonly List<string> _preDeclaredSymbols = new();
        private CompilationUnit _unit = null;
        private readonly Dictionary<string, object> _flags = new()
        {
            ["output"]      = null, // output filename
            ["target"]      = null, // extension
            ["means"]       = null, // extension
            ["mode"  ]      = null, // debug | release
            ["src"   ]      = null, // file to compile
            ["args"  ]      = null, // arguments
        };

        private string[] GetAllFilesInFolder(string directory)
        {
            var folder = new List<string>();
            folder.AddRange(Directory.GetFiles(directory));

            foreach (var subdirectory in Directory.GetDirectories(directory))
                folder.AddRange(GetAllFilesInFolder(subdirectory));

            return folder.ToArray();
        }

        private string[] GetFiles()
        {
            var file = GetFlag<string[]>("src");

            if (file is null)
                CompilationTower.Throw("Undefined src to compile");

            return file;
        }

        private string GetOutputPath()
        {
            return Path.ChangeExtension(GetFlag<string>("output"), GetOutputExtension());
        }

        private void SetFlag(string flag, object value)
        {
            _flags[flag] = value;
        }

        private T GetFlag<T>(string flag)
        {
            return (T)_flags[flag];
        }

        private void SetDefault(string flag, object value)
        {
            if (IsDefault(flag))
                SetFlag(flag, value);
        }

        private bool IsDefault(string flag)
        {
            return GetFlag<object>(flag) is null;
        }

        private void DumpBytecode(LLVMModuleRef llvmmodule)
        {
            File.WriteAllText(GetOutputPath(), llvmmodule.ToString());
        }

        private void DumpAbstractSyntaxTree(INode head)
        {
            File.WriteAllText(GetOutputPath(), head.Dump());
        }

        private void DeclarePlatformSymbol()
        {
            DeclareSymbol(Environment.OSVersion.Platform switch
            {
                PlatformID.Unix => "unix",
                PlatformID.Win32NT => "nt"
            });

            DeclareSymbol(RuntimeInformation.ProcessArchitecture.ToString());
        }

        private void DeclarePreDeclaredSymbols()
        {
            for (var i = 0; i < _preDeclaredSymbols.Count; i++)
                DeclareSymbol(_preDeclaredSymbols[i]);
        }

        private void Build(bool loadArgs = true)
        {
            if (loadArgs)
            {
                ParseArguments();
                CheckForUnusableFlags("build", "args");
                DeclareCompilerSymbols();
            }

            var path = GetFiles();
            var output = GetFlag<string>("output");
            var target = GetFlag<CompilationTarget>("target");

            _unit = new CompilationUnit(output, path);

            DeclareSymbol(GetFlag<CompilationMode>("mode").ToString());
            DeclareSymbol(target.ToString());

            switch (target)
            {
                case CompilationTarget.BC:
                    CompileLLVM(onlyBitcode: true);
                    break;
                case CompilationTarget.LL:
                    PrintAlertsIfNeeded(_unit.GenerateLLVMIR(out var llvmmodule));
                    DumpBytecode(llvmmodule);
                    break;
                case CompilationTarget.TAST:
                    PrintAlertsIfNeeded(_unit.GenerateTAST(out var head));
                    DumpAbstractSyntaxTree(head);
                    break;
                case CompilationTarget.AST:
                    PrintAlertsIfNeeded(_unit.GenerateAST(out head));
                    DumpAbstractSyntaxTree(head);
                    break;
                case CompilationTarget.ASM:
                    if (GetMeans() is CompilationMeans.C)
                        CompileC("-S");
                    else
                        CompileLLVM("-S");
                    break;
                case CompilationTarget.EXE:
                    if (GetMeans() is CompilationMeans.C)
                        CompileC();
                    else
                        CompileLLVM();
                    break;
                case CompilationTarget.MIRJSON:
                case CompilationTarget.MIR:
                    PrintAlertsIfNeeded(_unit.GenerateIR(out var ir));
                    DumpMIR(ir, target is CompilationTarget.MIRJSON);
                    break;
                default:
                    CompilationTower.Throw("Unsupported target, try with another");
                    break;
            }
        }

        private CompilationMeans GetMeans()
        {
            return GetFlag<CompilationMeans>("means");
        }

        private void DeclareCompilerSymbols()
        {
            DeclarePlatformSymbol();
            DeclarePreDeclaredSymbols();
            SetDefaultIFNeeded();
        }

        private void CheckForUnusableFlags(string compilationAction, params string[] unusables)
        {
            foreach (var unusable in unusables)
                if (!IsDefault(unusable))
                    CompilationTower.Throw($"Unable to use flag '{unusable}' when compilation action is '{compilationAction}'");
        }

        private void DumpMIR(MIR ir, bool generatejson)
        {
            File.WriteAllText(GetOutputPath(), generatejson ? ir.DumpJSON() : ir.Dump());
        }

        private void DeclareSymbol(string name)
        {
            // CompilationTower.Todo("fix declaresymbol in compilation flags");
            // _unit.DeclareCompilerSymbol(name);
        }

        private void CompileLLVM(string flag = "", bool onlyBitcode = false)
        {
            _unit.CompileLLVMIR(
                (int)GetFlag<CompilationMode>("mode"),
                GetFlag<string>("output"),
                onlyBitcode,
                flag);
        }

        private void CompileC(string flag = "")
        {
            _unit.CompileC(
                (int)GetFlag<CompilationMode>("mode"),
                GetFlag<string>("output"),
                flag);
        }

        private void BuildRun()
        {
            ParseArguments();

            if (GetFlag<CompilationTarget>("target") is not CompilationTarget.EXE)
                CompilationTower.Throw("Unable to perform compilation action 'run' when target is not 'exe'");

            Build(false);

            var process = Process.Start(GetFlag<string>("output"), GetFlag<string>("args"));

            process.WaitForExit();
            Environment.ExitCode = process.ExitCode;
        }

        private static string CheckPath(string path)
        {
            if (!File.Exists(path))
                CompilationTower.Throw($"Unable to find path '{path}'");

            return path;
        }

        private static string[] CheckMugFiles(string[] sources)
        {
            foreach (var source in sources)
                CheckMugFile(source);

            return sources;
        }

        private static string CheckMugFile(string source)
        {
            if (source == ".")
                return source;

            CheckPath(source);

            if (!CompilationUnit.AllowedExtensions.Contains(Path.GetExtension(source)))
                CompilationTower.Throw($"Unable to recognize source file kind '{source}'");

            return source;
        }

        private void ConfigureFlag(string flag, object value)
        {
            if (!IsDefault(flag))
                CompilationTower.Throw($"Impossible to specify multiple times the flag '{flag}'");
            else
                SetFlag(flag, value);
        }

        private string NextArgument()
        {
            if (++_argumentSelector >= _arguments.Length)
                CompilationTower.Throw($"Expected a specification after flag '{_arguments[_argumentSelector-1][1..]}'");

            return _arguments[_argumentSelector];
        }

        public void SetArguments(string[] arguments)
        {
            _arguments = arguments;
        }

        private static CompilationTarget ParseCompilationTarget(string target)
        {
            for (var i = 0; i < _targets.Length; i++)
                if (_targets[i] == target)
                    return (CompilationTarget)i;
            
            CompilationTower.Throw($"Unable to recognize target '{target}'");
            return CompilationTarget.EXE;
        }

        private static CompilationMode GetMode(string mode)
        {
            switch (mode)
            {
                case "debug": return CompilationMode.Debug;
                case "release": return CompilationMode.Release;
                default:
                    CompilationTower.Throw($"Unable to recognize compilation mode '{mode}'");
                    return default;
            }
        }

        private static string GetExecutableExtension()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : null;
        }

        private string GetOutputExtension()
        {
            return $".{_targets[(int)GetFlag<CompilationTarget>("target")]}";
        }

        private void SetDefaultIFNeeded()
        {
            SetDefault("means", CompilationMeans.C);
            SetDefault("target", CompilationTarget.EXE);
            SetDefault("mode", CompilationMode.Debug);
            SetDefault("output",
                Path.ChangeExtension(
                    IsDefault("output") ?
                        GetDefaultOutput() :
                        GetFlag<string>("output"), GetOutputExtension())
                );
        }

        private string GetDefaultOutput()
        {
            var src = GetFlag<string[]>("src");

            if (src is null || src.Length > 1)
                return Path.GetFileName(Environment.CurrentDirectory);

            return Path.GetFileName(src.First());
        }

        private void InterpretArgument(string argument)
        {
            if (argument[0] == '*')
            {
                var arg = argument[1..];
                switch (arg)
                {
                    case "src":
                        ConfigureFlag(arg, CheckMugFiles(NextArgument().Split(' ')));
                        break;
                    case "means":
                        ConfigureFlag(arg, ParseCompilationMeans(NextArgument()));
                        break;
                    case "mode":
                        ConfigureFlag(arg, GetMode(NextArgument()));
                        break;
                    case "target":
                        ConfigureFlag(arg, ParseCompilationTarget(NextArgument()));
                        break;
                    case "output":
                        ConfigureFlag(arg, NextArgument());
                        break;
                    case "dec":
                        _preDeclaredSymbols.Add(NextArgument());
                        break;
                    case "args":
                        ConfigureFlag("args", NextArgument());
                        break;
                    case "":
                        CompilationTower.Throw("Invalid empty flag");
                        break;
                    default:
                        CompilationTower.Throw($"Unknown compiler flag '{arg}'");
                        break;
                }
            }
            else
                AddSourceFilename(CheckMugFile(argument));
        }

        private CompilationMeans ParseCompilationMeans(string value)
        {
            return value switch
            {
                "llvm" => CompilationMeans.LLVM,
                "c" => CompilationMeans.C,
                _ => error()
            };

            CompilationMeans error()
            {
                CompilationTower.Throw($"Invalid compilation means '{value}'");
                return default;
            }
        }

        private void Check()
        {
            ParseArguments();
            CheckForUnusableFlags("check", "output", "target", "mode", "args");
            DeclareCompilerSymbols();

            _unit = new CompilationUnit("", GetFiles());
            var e = _unit.GenerateIR(out _);
            PrintAlertsIfNeeded(e);
        }

        private void PrintAlertsIfNeeded(CompilationException e)
        {
            if (_unit.HasErrors())
                PrettyPrinter.PrintAlerts(e);
        }

        private void AddSourceFilename(string source)
        {
            var sources = source == "." ?
                GetAllFilesInFolder(Environment.CurrentDirectory) :
                IsDefault("src") ? new string[] { source } :
                GetFlag<string[]>("src").Append(source).ToArray();

            SetFlag("src", sources);
        }

        private void ParseArguments()
        {
            for (; _argumentSelector < _arguments.Length; _argumentSelector++)
                InterpretArgument(_arguments[_argumentSelector]);
        }


        internal static void PrintUsageAndHelp()
        {
            Console.Write(USAGE);
            Console.Write(HELP);
        }
        
        private static void PrintHelpFor(string flag)
        {
            switch (flag)
            {
                case "src":
                    Console.Write(SRC_HELP);
                    break;
                case "mode":
                    Console.Write(MODE_HELP);
                    break;
                case "target":
                    Console.Write(TARGET_HELP);
                    break;
                case "output":
                    Console.Write(OUTPUT_HELP);
                    break;
                case "dec":
                    Console.Write(DEC_HELP);
                    break;
                case "args":
                    Console.WriteLine(ARGS_HELP);
                    break;
                default:
                    CompilationTower.Throw($"Unkown compiler flag '{flag}'");
                    break;
            }
        
        }

        private void Help()
        {
            if (_arguments.Length > 1)
                CompilationTower.Throw("Too many arguments for the 'help' compilation action");
            else if (_arguments.Length == 1)
                PrintHelpFor(_arguments[_argumentSelector]);
            else
                PrintUsageAndHelp();
        }

        public void InterpretAction(string actionid)
        {
            switch (actionid)
            {
                case "build":
                    Build();
                    break;
                case "run":
                    BuildRun();
                    break;
                case "check":
                    Check();
                    break;
                case "help":
                    Help();
                    break;
                default:
                    CompilationTower.Throw($"Invalid compilation action '{actionid}'");
                    break;
            }
        }
    }
}
