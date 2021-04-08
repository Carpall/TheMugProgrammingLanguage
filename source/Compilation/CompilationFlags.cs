using LLVMSharp.Interop;
using Mug.Models.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Mug.Compilation
{
    public enum CompilationMode
    {
        Release = 3,
        Debug = 0
    }

    public enum CompilationTarget
    {
        Executable,
        Library, // not available yet
        Bitcode,
        Assembly,
        AbstractSyntaxTree,
        Bytecode
    }

    public class CompilationFlags
    {
        private const string USAGE = "\nUSAGE: mug <action> <file> <options>\n";
        private const string HELP = @"
Compilation Actions:
  - build: to compile a program, with the following default options: {target: exe, mode: debug, output: <file>.exe}
  - run: build and run
  - help: show this list or describes a compilation flag when one argument is passed

Compilation Flags:
  - src: source file to compile (one at time)
  - mode: the compilation mode: {release: fast and small exe, debug: faster compilation, slower exe, allows to use llvmdbg}
  - target: file format to generate: {exe, lib, bc, asm, ast, ll}
  - output: output file name

How To Use:
  - compilation action: it's a command to give to the compiler, only one compilation action for call
  - compilation flag: it's a directive to give to the compilatio action, each compilation flag must be preceded by 
*'
";
        private const string SRC_HELP = @"
USAGE: mug <action> <options> *src <file>

HELP: uses the next argument as source file to compile, curretly only one file at compilation supported
";
        private const string MODE_HELP = @"
USAGE: mug <action> <file> <options> *mode (debug | release)

HELP: uses the next argument as compilation mode:
  - debug: for a faster compilation, allows debugging with llvmdbg
  - release: for a faster runtime execution, supports code optiminzation
";
        private const string TARGET_HELP = @"
USAGE: mug <action> <file> <options> *target (exe | lib | bc | asm | ast | ll)

HELP: uses the next argument as compilation target:
  - exe: executable with platform specific extension
  - lib: dynamic link library
  - bc: llvm bitcode
  - asm: clang assembly
  - ast: abstract syntax tree
  - ll: llvm bytecode
";
        private const string DEC_HELP = @"
USAGE: mug <action> <file> <options> *dec symbol

HELP: uses the next argument as symbol to declare before the compilation:
";
        private const string OUTPUT_HELP = @"
USAGE: mug <action> <file> <options> *output <name>

HELP: uses the next argument as output file name. The extension is not required
";

        private readonly string[] _allowedExtensions = new[] { ".mug" };
        private string[] _arguments = null;
        private int _argumentSelector = 0;
        private readonly List<string> _preDeclaredSymbols = new();
        private CompilationUnit unit = null;
        private readonly Dictionary<string, object> _flags = new()
        {
            ["output"]      = null,
            ["target"]      = null,
            ["mode"]        = null,
            ["src"]         = null, // file to compile
        };

        private string[] GetAllFilesInFolder(string directory)
        {
            var folder = new List<string>();
            folder.AddRange(Directory.GetFiles(directory));

            foreach (var subdirectory in Directory.GetDirectories(directory))
                folder.AddRange(GetAllFilesInFolder(subdirectory));

            return folder.ToArray();
        }

        private object GetFile()
        {
            var file = GetFlag<string>("src");

            if (file is null)
                CompilationErrors.Throw("Undefined src to compile");

            return file != "." ? file : GetAllFilesInFolder(Environment.CurrentDirectory);
        }

        private string GetMainFile(object filenames)
        {
            if (filenames is string main)
                return main;

            var files = filenames as string[];

            if (files.Length == 0)
                CompilationErrors.Throw("Folder is empty");

            foreach (var file in files)
            {
                if (Path.GetFileName(file) == CompilationUnit.MainFileName)
                    return file;
            }

            CompilationErrors.Throw("Unable to find 'main.mug' in the folder");
            return default;
        }

        private string GetOutputPath()
        {
            return Path.ChangeExtension(IsDefault("output") ? GetMainFile(GetFile()) : GetFlag<string>("output"), GetOutputExtension());
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

        private void DumpBytecode(string path, LLVMModuleRef module)
        {
            File.WriteAllText(path, module.ToString());
        }

        private void DumpAbstractSyntaxTree(string path, INode head)
        {
            File.WriteAllText(path, head.Dump());
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
            for (int i = 0; i < _preDeclaredSymbols.Count; i++)
                DeclareSymbol(_preDeclaredSymbols[i]);
        }

        private void Build(bool loadArgs = true)
        {
            if (loadArgs)
                LoadArguments();

            var path = GetFile();
            GetMainFile(path); // checks

            unit = new CompilationUnit(path);

            DeclareSymbol(GetFlag<CompilationMode>("mode").ToString());
            DeclarePlatformSymbol();

            DeclarePreDeclaredSymbols();

            switch (GetFlag<CompilationTarget>("target"))
            {
                case CompilationTarget.Bitcode:
                    DeclareSymbol("bc");
                    Compile("", true);
                    break;
                case CompilationTarget.Bytecode:
                    DeclareSymbol("ll");
                    unit.Generate(false);
                    DumpBytecode(GetOutputPath(), unit.IRGenerator.Module);
                    break;
                case CompilationTarget.AbstractSyntaxTree:
                    DeclareSymbol("ast");
                    unit.GenerateAST();
                    DumpAbstractSyntaxTree(GetOutputPath(), unit.IRGenerator.Parser.Module);
                    break;
                case CompilationTarget.Assembly:
                    DeclareSymbol("asm");
                    Compile("-S");
                    break;
                case CompilationTarget.Executable:
                    DeclareSymbol("exe");
                    Compile();
                    break;
                default:
                    CompilationErrors.Throw("Unsupported target, try with another");
                    break;
            }
        }

        private void DeclareSymbol(string name)
        {
            unit.IRGenerator.DeclareCompilerSymbol(name);
        }

        private void Compile(string flag = "", bool onlyBitcode = false)
        {
            unit.Compile(
                (int)GetFlag<CompilationMode>("mode"),
                GetFlag<string>("output"),
                onlyBitcode,
                flag);
        }

        private void BuildRun()
        {
            LoadArguments();

            if (GetFlag<CompilationTarget>("target") != CompilationTarget.Executable)
                CompilationErrors.Throw("Unable to perform compilation action 'run' when target is not 'exe'");

            Build(false);

            var process = Process.Start(GetFlag<string>("output"));

            process.WaitForExit();

            Environment.Exit(process.ExitCode);
        }

        private string CheckPath(string path)
        {
            if (!File.Exists(path))
                CompilationErrors.Throw($"Unable to find path '{path}'");

            return path;
        }

        private string CheckMugFile(string src)
        {
            if (src == ".")
                return src;

            CheckPath(src);

            if (!_allowedExtensions.Contains(Path.GetExtension(src)))
                CompilationErrors.Throw($"Unable to recognize source file kind '{src}'");

            return src;
        }

        private void ConfigureFlag(string flag, object value)
        {
            if (!IsDefault(flag))
                CompilationErrors.Throw($"Impossible to specify multiple times the flag '{flag}'");
            else
                SetFlag(flag, value);
        }

        private string NextArgument()
        {
            if (++_argumentSelector >= _arguments.Length)
                CompilationErrors.Throw($"Expected a specification after flag '{_arguments[_argumentSelector-1][1..]}'");

            return _arguments[_argumentSelector];
        }

        public void SetArguments(string[] arguments)
        {
            _arguments = arguments;
        }

        private CompilationTarget GetTarget(string target)
        {
            switch (target)
            {
                case "exe": return CompilationTarget.Executable;
                case "lib": CompilationErrors.Throw("Library traget is not supported yet"); return CompilationTarget.Library;
                case "bc": return CompilationTarget.Bitcode;
                case "asm": return CompilationTarget.Assembly;
                case "ast": return CompilationTarget.AbstractSyntaxTree;
                case "ll": return CompilationTarget.Bytecode;
                default:
                    CompilationErrors.Throw($"Unable to recognize target '{target}'");
                    return CompilationTarget.Executable;
            }
        }

        private CompilationMode GetMode(string mode)
        {
            switch (mode)
            {
                case "debug": return CompilationMode.Debug;
                case "release": return CompilationMode.Release;
                default:
                    CompilationErrors.Throw($"Unable to recognize compilation mode '{mode}'");
                    return default;
            }
        }

        private static string GetExecutableExtension()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT ? ".exe" : null;
        }

        private string GetOutputExtension()
        {
            return GetFlag<CompilationTarget>("target") switch
            {
                CompilationTarget.Assembly => ".s",
                CompilationTarget.Executable => GetExecutableExtension(),
                CompilationTarget.Bytecode => ".ll",
                CompilationTarget.AbstractSyntaxTree => ".ast",
                _ => "?"
            };
        }

        private void SetDefaultIFNeeded()
        {
            SetDefault("target", CompilationTarget.Executable);
            SetDefault("mode", CompilationMode.Debug);
            SetDefault("output", Path.ChangeExtension(GetFile() is string s ? s : Path.GetFileName(Environment.CurrentDirectory), GetOutputExtension()));
        }

        private void InterpretArgument(string argument)
        {
            if (argument[0] == '*')
            {
                var arg = argument[1..];
                switch (arg)
                {
                    case "src":
                        ConfigureFlag(arg, CheckMugFile(NextArgument()));
                        break;
                    case "mode":
                        ConfigureFlag(arg, GetMode(NextArgument()));
                        break;
                    case "target":
                        ConfigureFlag(arg, GetTarget(NextArgument()));
                        break;
                    case "output":
                        ConfigureFlag(arg, NextArgument());
                        break;
                    case "dec":
                        _preDeclaredSymbols.Add(NextArgument());
                        break;
                    case "":
                        CompilationErrors.Throw("Invalid empty flag");
                        break;
                    default:
                        CompilationErrors.Throw($"Unknown compiler flag '{arg}'");
                        break;
                }
            }
            else
                ConfigureFlag("src", CheckMugFile(argument));
        }

        private void LoadArguments()
        {
            for (; _argumentSelector < _arguments.Length; _argumentSelector++)
                InterpretArgument(_arguments[_argumentSelector]);

            SetDefaultIFNeeded();
        }

        private void PrintUsageAndHelp()
        {
            Console.Write(USAGE);
            Console.Write(HELP);
        }
        
        private void PrintHelpFor(string flag)
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
                default:
                    CompilationErrors.Throw($"Unkown compiler flag '{flag}'");
                    break;
            }
        }

        private void Help()
        {
            if (_arguments.Length > 1)
                CompilationErrors.Throw("Too many arguments for the 'help' compilation action");
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
                case "help":
                    Help();
                    break;
                default:
                    CompilationErrors.Throw($"Invalid compilation action '{actionid}'");
                    break;
            }
        }
    }
}
