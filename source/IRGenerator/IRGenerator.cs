using LLVMSharp.Interop;
using Mug.Compilation;
using Mug.Compilation.Symbols;
using Mug.Models.Generator.Emitter;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using Mug.Models.Parser.NodeKinds.Directives;
using Mug.Models.Parser.NodeKinds.Statements;
using Mug.MugValueSystem;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mug.Models.Generator
{
    public class IRGenerator
    {
        public LLVMModuleRef Module { get; set; }

        public readonly MugParser Parser;
        public readonly SymbolTable Table;

        internal readonly List<string> IllegalTypes = new();
        internal List<(string name, MugValueType type)> GenericParameters = new();
        internal List<string> Paths = new(); /// to put in map

        private readonly Dictionary<string, List<FunctionNode>> _genericFunctions = new();
        private readonly bool _isMainModule = false;

        internal int SizeOfPointer => (int)LLVMTargetDataRef.FromStringRepresentation(Module.DataLayout)
                    .StoreSizeOfType(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0));

        internal const string EntryPointName = "main";
        internal const string AsOperatorOverloading = "as";

        public string LocalPath
        {
            get
            {
                return Path.GetFullPath(Parser.Lexer.ModuleName);
            }
        }

        IRGenerator(MugParser parser, string moduleName, bool isMainModule)
        {
            Parser = parser;
            Module = LLVMModuleRef.CreateWithName(moduleName);
            _isMainModule = isMainModule;
            Table = new(this);
        }

        public IRGenerator(string moduleName, string source, bool isMainModule) : this(new MugParser(moduleName, source), moduleName, isMainModule)
        {
        }

        public IRGenerator(MugParser parser, bool isMainModule) : this(parser, parser.Lexer.ModuleName, isMainModule)
        {
        }

        public void Error(Range position, string error)
        {
            Parser.Lexer.Throw(position, error);
        }

        public void Report(Range position, string error)
        {
            Parser.Lexer.DiagnosticBag.Report(position, error);
        }

        internal static string GetPlural(int ammout)
        {
            return ammout > 1 ? "s" : "";
        }

        internal static string ExpectTypeMessage(MugValueType expectedType, MugValueType type)
        {
            return $"Expected type '{expectedType}', got '{type}'";
        }

        /// <summary>
        /// the function launches an exception and returns a generic value,
        /// this function comes in statement switch in expressions
        /// </summary>
        internal T NotSupportedType<T>(string type, Range position)
        {
            return Error<T>(position, $"'{type}' type is not supported yet");
        }

        internal T Error<T>(Range position, string error)
        {
            Error(position, error);
            return default;
        }

        public ulong StringCharToIntChar(string value)
        {
            return Convert.ToUInt64(Convert.ToChar(value));
        }

        internal bool IsIllegalType(string name)
        {
            for (int i = 0; i < IllegalTypes.Count; i++)
                if (IllegalTypes[i] == name)
                    return true;

            return false;
        }

        /// <summary>
        /// calls the function <see cref="TypeToMugType(MugType, Range)"/> for each parameter in the past array
        /// </summary>
        internal MugValueType[] ParameterTypesToMugTypes(List<ParameterNode> parameterTypes)
        {
            var result = new MugValueType[parameterTypes.Count];
            for (int i = 0; i < parameterTypes.Count; i++)
                result[i] = parameterTypes[i].Type.ToMugValueType(this);

            return result;
        }

        /// <summary>
        /// calls the function <see cref="TypeToMugType(MugType, Range)"/> for each parameter in the past array
        /// </summary>
        internal MugValueType[] MugTypesToMugValueTypes(List<MugType> types)
        {
            var result = new MugValueType[types.Count];
            for (int i = 0; i < types.Count; i++)
                result[i] = types[i].ToMugValueType(this);

            return result;
        }

        internal bool IsGenericParameter(string name, out MugValueType genericParameter)
        {
            for (int i = 0; i < GenericParameters.Count; i++)
                if (GenericParameters[i].Item1 == name)
                {
                    genericParameter = GenericParameters[i].Item2;
                    return true;
                }

            genericParameter = new();
            return false;
        }

        internal static LLVMTypeRef[] MugTypesToLLVMTypes(MugValueType[] parameterTypes)
        {
            var result = new LLVMTypeRef[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
                result[i] = parameterTypes[i].LLVMType;

            return result;
        }

        private void PopIllegalType()
        {
            IllegalTypes.RemoveAt(IllegalTypes.Count - 1);
        }

        private void PushIllegalType(string illegalType)
        {
            IllegalTypes.Add(illegalType);
        }

        internal MugValueType EvaluateEnumError(MugType error, MugType type)
        {
            // todo: cache generated enum error
            var errortype = error.ToMugValueType(this);
            var successtype = type.ToMugValueType(this);

            if (!errortype.IsEnum())
                Error(error.Position, "Expected enum type");

            return MugValueType.EnumError(new EnumErrorInfo()
            {
                Name = $"{error}!{type}",
                ErrorType = errortype,
                SuccessType = successtype,
                LLVMValue = LLVMTypeRef.CreateStruct(new[] { LLVMTypeRef.Int8, successtype.LLVMType }, true)
            });
        }

        internal MugValue? EvaluateStruct(TypeStatement type, MugValueType[] generics, Range position)
        {
            if (type.Generics.Count != generics.Length)
            {
                Report(position, "Passed wrong number of generics");
                return null;
            }

            PushIllegalType(type.Name);

            var oldGenericParameters = GenericParameters;

            GenericParameters = new();
            for (int i = 0; i < type.Generics.Count; i++)
                GenericParametersAdd((type.Generics[i].Value, generics[i]), type.Generics[i].Position);

            var fields = new string[type.Body.Count];
            var structModel = new MugValueType[type.Body.Count];
            var fieldPositions = new Range[type.Body.Count];

            for (int i = 0; i < type.Body.Count; i++)
            {
                var field = type.Body[i];

                if (fields.Contains(field.Name))
                {
                    Report(field.Position, "Already declared field");
                    return null;
                }

                fields[i] = field.Name;
                structModel[i] = field.Type.ToMugValueType(this);
                fieldPositions[i] = field.Position;
            }

            var structuretype = MugValueType.Struct($"{type.Name}{(type.Generics.Count > 0 ? $"<{string.Join(", ", generics)}>" : "")}", structModel, fields, fieldPositions);
            var structsymbol = MugValue.Struct(Module.AddGlobal(structuretype.LLVMType, type.Name), structuretype);

            GenericParameters = oldGenericParameters;

            PopIllegalType();

            return structsymbol;
        }

        internal void GenericParametersAdd((string name, MugValueType type) genericParameter, Range position)
        {
            if (GenericParameters.FindIndex(elem => elem.name == genericParameter.name) != -1)
                Error(position, "Already declared generic parameter");

            GenericParameters.Add(genericParameter);
        }

        internal void ClearGenericParameters()
        {
            GenericParameters.Clear();
        }

        public void DeclareCompilerSymbol(string symbol, bool hasGoodPosition = false, Range position = new())
        {
            symbol = symbol.ToLower();

            if (Table.CompilerSymbolIsDeclared(symbol))
            {
                var error = $"Compiler symbol '{symbol}' is already declared";

                if (!hasGoodPosition)
                    CompilationErrors.Throw(error);

                Error(position, error);
            }

            Table.DeclareCompilerSymbol(symbol, position);
        }

        /// <summary>
        /// converts a boolean value in string format to one in int format
        /// </summary>
        public ulong StringBoolToIntBool(string value)
        {
            // converts for first string "true" or "false" to a boolean value, then to a ulong, so 0 or 1
            return Convert.ToUInt64(Convert.ToBoolean(value));
        }

        internal LLVMValueRef[] MugValuesToLLVMValues(MugValue[] values)
        {
            var result = new LLVMValueRef[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = values[i].LLVMValue;

            return result;
        }

        /// <summary>
        /// the function checks that all the past types are the same, therefore compatible with each other
        /// </summary>
        internal void ExpectSameTypes(MugValueType firstType, Range position, string error, params MugValueType[] types)
        {
            for (int i = 0; i < types.Length; i++)
                if (!firstType.Equals(types[i]))
                    Report(position, error);
        }

        internal void ExpectBoolType(MugValueType type, Range position)
        {
            ExpectSameTypes(type, position, $"Expected 'u1' type, got '{type}'", MugValueType.Bool);
        }

        /// <summary>
        /// same of <see cref="ExpectNonVoidType(LLVMTypeRef, Range)"/> but tests a <see cref="MugType"/> instead of
        /// a <see cref="LLVMTypeRef"/>
        /// </summary>
        public void ExpectNonVoidType(MugType type, Range position)
        {
            if (type.Kind == TypeKind.Void)
                Error(position, "In the current context 'void' is not allowed");
        }

        /// <summary>
        /// launches a compilation-error if the type that is passed is of void type:
        /// this function is used in all contexts where the type void is not allowed,
        /// for example in the declaration of variables
        /// </summary>
        internal void ExpectNonVoidType(LLVMTypeRef type, Range position)
        {
            if (type == LLVMTypeRef.Void)
                Error(position, "Expected a non-void type");
        }

        /*private FunctionNode GetGenericFunctionSymbol(string name, MugValueType? basetype, MugValueType[] parameters, MugValueType[] genericsInput, Range position)
        {
            if (!_genericFunctions.TryGetValue(name, out var overloads))
            {
                Error(position, "Undeclared generic function");
                throw new();
            }

            for (int i = 0; i < overloads.Count; i++)
            {
                var types = new MugValueType[overloads[i].ParameterList.Length];

                var oldGenericParamters = GenericParameters;
                GenericParameters = new();

                for (int j = 0; j < overloads[i].Generics.Count; j++)
                    GenericParametersAdd((overloads[i].Generics[j].Value, genericsInput[j]), overloads[i].Generics[j].Position);

                var overloadBasetype = overloads[i].Base?.Type.ToMugValueType(this);

                for (int j = 0; j < overloads[i].ParameterList.Length; j++)
                    types[j] = overloads[i].ParameterList.Parameters[j].Type.ToMugValueType(this);

                GenericParameters = oldGenericParamters;

                if (parameters.Length != types.Length)
                    continue;
                
                int h = 0;

                for (; h < types.Length; h++)
                {
                    if (!parameters[h].Equals(types[h]))
                        goto end;
                }

                if (basetype.HasValue)
                    if (!basetype.Value.Equals(overloadBasetype.Value))
                        goto end;

                return overloads[i];
            end:;
            }

            Error(position, "Undeclared generic function");
            throw new();
        }*/

        /*internal FunctionSymbol EvaluateFunction(
            string name,
            MugValueType? basetype,
            MugValueType[] parameters,
            MugValueType[] genericsInput,
            Range position,
            bool isAsOperaor = false)
        {
            var symbol = Table.GetFunction(name, new UndefinedFunctionID(basetype, genericsInput, parameters), out var index, position);

            if (genericsInput.Length > 0)
            {
            }

            if (symbol is FunctionSymbol || symbol is null)
                return (FunctionSymbol)symbol;

            return EvaluateFunction(name, ((FunctionPrototypeIdentifier)symbol).Prototype, index, genericsInput, true, position);
        }*/

        /*/// <summary>
        /// defines the body of a function by taking from the declared symbols its own previously defined symbol,
        /// to allow the call of a method declared under the caller.
        /// see the first part of the Generate function
        /// </summary>
        private FunctionSymbol EvaluateFunction(string name, FunctionNode function, int index, MugValueType[] genericsInput, bool ispublic, Range position)
        {
            if (function.Generics.Count != genericsInput.Length)
                Error(position, "Incorrect number of generic parameters");

            var oldGenericParameters = GenericParameters;

            GenericParameters = new();
            for (int i = 0; i < function.Generics.Count; i++)
                GenericParametersAdd((function.Generics[i].Value, genericsInput[i]), function.Generics[i].Position);

            var baseoffset = Convert.ToInt32(function.Base.HasValue);

            var paramTypes = new MugValueType[function.ParameterList.Length + baseoffset];
            var retType = function.ReturnType.ToMugValueType(this);

            var types = ParameterTypesToMugTypes(function.ParameterList.Parameters);

            if (function.Base.HasValue)
                paramTypes[0] = function.Base.Value.Type.ToMugValueType(this);

            for (int i = 0; i < types.Length; i++)
                paramTypes[i + baseoffset] = types[i];

            var llvmfunction = Module.AddFunction(name, LLVMTypeRef.CreateFunction(
                retType.LLVMType,
                MugTypesToLLVMTypes(paramTypes)));

            var func = new FunctionSymbol(
                Convert.ToBoolean(baseoffset) ? paramTypes[0] : null, genericsInput, paramTypes, retType, MugValue.From(llvmfunction, retType));

            Table.DefineFunctionSymbol(
                name,
                index,
                func);

            // basic block, won't be emitted any block because the name is empty
            var entry = llvmfunction.AppendBasicBlock("");

            var emitter = new MugEmitter(this, entry, false);

            emitter.Builder.PositionAtEnd(entry);

            var generator = new LocalGenerator(this, ref llvmfunction, ref function, ref emitter);
            generator.Generate();

            // if the type is void check if the last statement was ret, if it was not ret add one implicitly
            if (llvmfunction.LastBasicBlock.Terminator.IsAReturnInst.Handle == IntPtr.Zero &&
                function.ReturnType.Kind == TypeKind.Void)
                generator.AddImplicitRetVoid();

            GenericParameters = oldGenericParameters;

            return func;
        }*/

        /// <summary>
        /// check if an id is equal to the id of the entry point and if the parameters are 0,
        /// to allow overload of the main function
        /// </summary>
        internal bool IsEntryPoint(string name, int paramsLen)
        {
            return
                name == EntryPointName && paramsLen == 0;
        }

        private void ReadModule(string filename)
        {
            unsafe
            {
                LLVMOpaqueMemoryBuffer* memoryBuffer;
                LLVMOpaqueModule* module;
                sbyte* message;

                using (var marshalledFilename = new MarshaledString(filename))
                    if (LLVM.CreateMemoryBufferWithContentsOfFile(marshalledFilename, &memoryBuffer, &message) != 0)
                        CompilationErrors.Throw($"Unable to open file: '{filename}':\n{new string(message)}");

                if (LLVM.ParseBitcode(memoryBuffer, &module, &message) != 0)
                    CompilationErrors.Throw($"Unable to parse file: '{filename}':\n{new string(message)}");

                if (LLVM.LinkModules2(Module, module) != 0)
                    CompilationErrors.Throw($"Unable to link file: '{filename}', with the main module");
            }
        }

        private static int _tempFileCounter = 0;

        internal static string TempFile(string extension)
        {
            var dir = Path.Combine(Path.GetTempPath(), "mug");
            var file = Path.ChangeExtension(Path.Combine(dir, "tmp" + _tempFileCounter++), extension);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return file;
        }

        private void EmitFunctionPrototype(FunctionPrototypeNode prototype)
        {
            if (prototype.Generics.Count > 0)
                Error(prototype.Position, "Function prototypes cannot have generic parameters");

            var code = prototype.Pragmas.GetPragma("code");
            var header = prototype.Pragmas.GetPragma("header");
            var clangArgs = prototype.Pragmas.GetPragma("clang_args");
            var ext = prototype.Pragmas.GetPragma("ext");

            if (code != "")
            {
                if (header != "")
                    Error(prototype.Position, "Pragam 'code' is in conflict with 'header'");

                if (ext == "")
                    ext = ".c";

                var path = TempFile(ext);

                File.WriteAllText(path, code);

                IncludeCHeader(path, clangArgs);
            }

            if (header != "")
            {
                if (ext != "")
                    Error(prototype.Position, "Pragam 'header' is in conflict with 'ext'");

                var path = Path.GetFullPath(header, Path.GetDirectoryName(LocalPath));

                if (!AlreadyIncluded(path))
                {
                    EmitIncludeGuard(path);
                    IncludeCHeader(path, clangArgs);
                }
            }

            prototype.Pragmas.SetExtern(prototype.Name);

            var parameters = ParameterTypesToMugTypes(prototype.ParameterList.Parameters);
            // search for the function
            var function = Module.GetNamedFunction(prototype.Pragmas.GetPragma("extern"));

            var type = prototype.Type.ToMugValueType(this);
            
            // if the function is not declared yet
            if (function.Handle == IntPtr.Zero)
                // declares it
                function = Module.AddFunction(prototype.Pragmas.GetPragma("extern"),
                        LLVMTypeRef.CreateFunction(
                            type.LLVMType,
                            MugTypesToLLVMTypes(parameters)));

            // adding a new symbol
            Table.DeclareFunctionSymbol(
                prototype.Name,
                new FunctionSymbol(null, Array.Empty<MugValueType>(), parameters, type, MugValue.From(function, type)),
                prototype.Position);
        }

        private void IncludeCHeader(string path, string clangArgs)
        {
            var bc = TempFile("bc");

            // compiling c code to llvm bit code
            CompilationUnit.CallClang($"-emit-llvm -c {path} -o {bc} {clangArgs}", 3);

            // loading bitcode file
            ReadModule(bc);
        }

        private bool AlreadyIncluded(string path)
        {
            return Paths.Contains(path);
        }

        private void EmitIncludeGuard(string path)
        {
            // pragma once
            Paths.Add(path);
        }

        //////////////// tofix

        private void MergeSymbols(ref CompilationUnit unit)
        {
            /*for (int i = 0; i < unit.IRGenerator.Map.Count; i++)
            {
                var symbol = unit.IRGenerator.Map[i];

                if (symbol.IsPublic)
                {
                    symbol.IsPublic = false;
                    Map(symbol);
                }
            }

            for (int i = 0; i < unit.IRGenerator._genericFunctions.Count; i++)
            {
                var symbol = unit.IRGenerator._genericFunctions.Values.ElementAt(i);

                for (int j = 0; j < symbol.Count; j++)
                {
                    if (symbol[j].Modifier == TokenKind.KeyPub)
                    {
                        symbol[j].Modifier = TokenKind.KeyPriv;
                        DeclareGenericFunctionSymbol(symbol[j]);
                    }
                }
            }*/
            throw new();
        }

        private void EmitImport(ImportDirective import)
        {
            if (import.Member is not Token)
                Error(import.Position, "Unsupported import member");

            CompilationUnit unit = null;

            if (import.Mode == ImportMode.FromPackages) // dirof(mug.exe)/include/
            {
                // compilerpath\include\package.mug
                var path = AppDomain.CurrentDomain.BaseDirectory + "include\\" + ((Token)import.Member).Value + ".mug";

                if (AlreadyIncluded(path))
                    return;

                EmitIncludeGuard(path);

                unit = new CompilationUnit(path, false, false);

                if (unit.FailedOpeningPath)
                    Error(import.Member.Position, "Unable to find package");
            }
            else
            {
                var path = (Token)import.Member;
                var filekind = Path.GetExtension(path.Value);
                var fullpath = Path.GetFullPath(path.Value, Path.GetDirectoryName(LocalPath));

                if (AlreadyIncluded(fullpath))
                    return;

                EmitIncludeGuard(fullpath);
                var extensionPosition = (import.Member.Position.Start.Value + path.Value.Length - filekind.Length + 2)..(import.Member.Position.End.Value - 1);

                switch (filekind) {
                    case ".bc": // llvm bitcode file
                        ReadModule(fullpath);
                        return;
                    case ".mug": // dirof(file.mug)
                        unit = new CompilationUnit(fullpath, false, false);

                        if (unit.FailedOpeningPath)
                            Error(import.Member.Position, "Unable to open source file");
                        break;
                    case ".cpp":
                    case ".c":
                        IncludeCHeader(fullpath, "");
                        return;
                    case ".h":
                        Error(extensionPosition, "LLVM Bitcode reader cannot parse a llvm bitcode module generated from an header, please change extension to '.c'");
                        throw new();
                    default:
                        Error(extensionPosition, "Unrecognized file kind");
                        throw new();
                }
            }

            // pass the current module to generate the llvm code together by the irgenerator
            unit.IRGenerator.Paths = Paths;
            unit.IRGenerator.Module = Module;
            unit.Generate();
            MergeSymbols(ref unit);
        }

        private TokenKind GetValueTokenKindFromType(MugValueTypeKind kind, Range position)
        {
            return kind switch
            {
                MugValueTypeKind.Void => Error<TokenKind>(position, "Enum base type must be a non-void type"),
                MugValueTypeKind.String => TokenKind.ConstantString,
                MugValueTypeKind.Int8 or MugValueTypeKind.Int32 or MugValueTypeKind.Int64 => TokenKind.ConstantDigit,
                MugValueTypeKind.Bool => TokenKind.ConstantBoolean,
                MugValueTypeKind.Char => TokenKind.ConstantChar,
                _ => Error<TokenKind>(position, "Invalid enum base type")
            };
        }

        private void CheckEnum(ref EnumStatement enumstatement,  MugValueType basetype)
        {
            var expectedValue = GetValueTokenKindFromType(basetype.TypeKind, enumstatement.Position);
            var members = new List<string>();

            for (int i = 0; i < enumstatement.Body.Count; i++)
            {
                var member = enumstatement.Body[i];

                if (member.Value.Kind != expectedValue)
                    Report(member.Position, $"Expected type '{basetype}'");

                if (members.Contains(member.Name))
                    Report(member.Position, "Member already declared");

                members.Add(member.Name);
            }
        }

        private void EmitEnum(EnumStatement enumstatement)
        {
            var basetype = enumstatement.BaseType.ToMugValueType(this);

            CheckEnum(ref enumstatement, basetype);
            
            var type = MugValueType.Enum(basetype, enumstatement);

            Table.DeclareEnumType(
                enumstatement.Name,
                MugValue.Enum(type),
                enumstatement.Position);
        }

        private void MergeTree(NodeBuilder body)
        {
            foreach (var member in body.Nodes)
                RecognizeMember(member);
        }

        internal bool EvaluateCompTimeExprAndGetResult(CompTimeExpression comptimeExpr)
        {
            bool result = true;
            var lastOP = new Token(TokenKind.Bad, null, new());

            foreach (var token in comptimeExpr.Expression)
            {
                if (token.Kind != TokenKind.Identifier)
                    lastOP = token;
                else
                {
                    var symbolResult = Table.CompilerSymbolIsDeclared(token.Value);

                    if (lastOP.Kind == TokenKind.BooleanOR)
                        result |= symbolResult;
                    else
                        result &= symbolResult;
                }
            }

            return result;
        }

        private void EmitCompTimeWhen(CompTimeWhenStatement when)
        {
            if (EvaluateCompTimeExprAndGetResult(when.Expression))
                MergeTree((NodeBuilder)when.Body);
        }

        /*private void DeclareGenericFunctionSymbol(FunctionNode function)
        {
            var symbol = $"{new string('.', Convert.ToInt32(function.Base.HasValue))}{function.Name}<{new string('.', function.Generics.Count)}>";

            _genericFunctions.TryAdd(symbol, new());

            var types = new MugType[function.ParameterList.Length];

            for (int i = 0; i < function.ParameterList.Length; i++)
                types[i] = function.ParameterList.Parameters[i].Type;

            var f = _genericFunctions[symbol];

            for (int i = 0; i < f.Count; i++)
            {
                if (f[i].Name == function.Name && f[i].ParameterList.Length == function.ParameterList.Length)
                {
                    var ftypes = new MugType[f[i].ParameterList.Length];

                    for (int j = 0; j < f[i].ParameterList.Length; j++)
                        ftypes[j] = f[i].ParameterList.Parameters[j].Type;

                    for (int j = 0; j < types.Length; j++)
                        if (!types[j].Equals(ftypes[j]))
                            goto end;

                    Error(function.Position, "Function overload already declared");
                end:;
                }
            }

            f.Add(function);
        }
*/
        private EnumErrorStatement CheckEnumError(EnumErrorStatement enumerror)
        {
            var members = new List<string>();

            for (int i = 0; i < enumerror.Body.Count; i++)
            {
                if (members.Contains(enumerror.Body[i].Value))
                    Error(enumerror.Body[i].Position, "Already declared member");

                members.Add(enumerror.Body[i].Value);
            }

            return enumerror;
        }

        /// <summary>
        /// recognize the type of the AST node and depending on the type call methods
        /// to convert it to the corresponding low-level code
        /// </summary>
        private void RecognizeMember(INode member)
        {
            switch (member)
            {
                case FunctionNode function:
                    Table.DeclaredFunctions.Add(function);
                    break;
                case FunctionPrototypeNode prototype:
                    EmitFunctionPrototype(prototype);
                    break;
                case TypeStatement structure:
                    Table.DeclaredTypes.Add(structure);
                    break;
                case EnumStatement enumstatement:
                    EmitEnum(enumstatement);
                    break;
                case ImportDirective import:
                    EmitImport(import);
                    break;
                case CompTimeWhenStatement comptimewhen:
                    EmitCompTimeWhen(comptimewhen);
                    break;
                case DeclareDirective declare:
                    DeclareCompilerSymbol(declare.Symbol.Value, true, declare.Position);
                    break;
                case EnumErrorStatement enumerror:
                    Table.DeclareEnumErrorType(enumerror, enumerror.Position);
                    break;
                default:
                    Error(member.Position, "Declaration not supported yet");
                    break;
            }
        }
        
        internal MugValue EvaluateFunction(FunctionNode function, MugValueType[] generics)
        {
            var oldgenerics = GenericParameters;

            GenericParameters = new();
            for (int i = 0; i < function.Generics.Count; i++)
                GenericParametersAdd((function.Generics[i].Value, generics[i]), function.Generics[i].Position);

            var baseoffset = Convert.ToInt32(function.Base.HasValue);

            var paramTypes = new MugValueType[function.ParameterList.Length + baseoffset];
            var retType = function.ReturnType.ToMugValueType(this);

            var types = ParameterTypesToMugTypes(function.ParameterList.Parameters);

            if (function.Base.HasValue)
                paramTypes[0] = function.Base.Value.Type.ToMugValueType(this);

            for (int i = 0; i < types.Length; i++)
                paramTypes[i + baseoffset] = types[i];

            var llvmfunction = Module.AddFunction(function.Name, LLVMTypeRef.CreateFunction(
                retType.LLVMType,
                MugTypesToLLVMTypes(paramTypes)));

            var func = MugValue.From(llvmfunction, retType);

            // basic block, won't be emitted any block because the name is empty
            var entry = llvmfunction.AppendBasicBlock("");

            var emitter = new MugEmitter(this, entry, false);

            emitter.Builder.PositionAtEnd(entry);

            var generator = new LocalGenerator(this, ref llvmfunction, ref function, ref emitter);
            generator.Generate();

            // if the type is void check if the last statement was ret, if it was not ret add one implicitly
            if (llvmfunction.LastBasicBlock.Terminator.IsAReturnInst.Handle == IntPtr.Zero &&
                function.ReturnType.Kind == TypeKind.Void)
                generator.AddImplicitRetVoid();

            GenericParameters = oldgenerics;

            return func;
        }

        private bool CheckAsOperator(FunctionNode function)
        {
            return
                !function.Base.HasValue &&
                function.Generics.Count == 0 &&
                function.ParameterList.Length == 1 &&
                function.ReturnType.Kind != TypeKind.EnumError && function.ReturnType.Kind != TypeKind.Void;
        }

        private void GenerateAsOperator(FunctionNode function)
        {
            if (!CheckAsOperator(function))
            {
                // make sure it has not base, no generic parameters, no more than one parameter and and its return type is not an enum error or void
                Report(function.Position, "Wrong implementation of 'as' operator");
                return;
            }

            var functionIdentifier = new FunctionSymbol(
                    null,
                    Array.Empty<MugValueType>(),
                    ParameterTypesToMugTypes(function.ParameterList.Parameters),
                    function.ReturnType.ToMugValueType(this), EvaluateFunction(function, Array.Empty<MugValueType>()));

            Table.DeclareAsOperators(functionIdentifier, function.Position);
        }

        private void GenerateFunctions()
        {
            foreach (var function in Table.DeclaredFunctions)
            {
                if (function.Name == AsOperatorOverloading)
                {
                    GenerateAsOperator(function);
                    continue;
                }

                if (function.Generics.Count > 0)
                {
                    Table.DeclareGenericFunction(function);
                    continue;
                }

                var functionIdentifier = new FunctionSymbol(
                    function.Base?.Type.ToMugValueType(this),
                    Array.Empty<MugValueType>(),
                    ParameterTypesToMugTypes(function.ParameterList.Parameters),
                    function.ReturnType.ToMugValueType(this), EvaluateFunction(function, Array.Empty<MugValueType>()));

                Table.DeclareFunctionSymbol(function.Name, functionIdentifier, function.Position);
            }
        }

        private void GenerateTypes()
        {
            foreach (var type in Table.DeclaredTypes)
            {
                if (type.Generics.Count > 0)
                {
                    Table.DeclareGenericType(type);
                    continue;
                }

                var generics = Array.Empty<MugValueType>();
                var evaluated = EvaluateStruct(type, generics, type.Position);
                if (!evaluated.HasValue)
                    continue;

                var typeIdentifier = new TypeSymbol(generics, evaluated.Value);

                Table.DeclareType(type.Name, typeIdentifier, type.Position);
            }
        }

        /// <summary>
        /// declares the prototypes of all the global members, then defines them,
        /// to allow the use of a member declared under the member that uses it
        /// </summary>
        public void Generate()
        {
            // prototypes' declaration
            foreach (var member in Parser.Module.Members.Nodes)
                RecognizeMember(member);

            // generate all types here, generic types are saved for next use
            GenerateTypes();

            // generate all functions here
            GenerateFunctions();

            // checking for errors
            Parser.Lexer.CheckDiagnostic();
        }
    }
}
