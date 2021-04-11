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
using System.Runtime.CompilerServices;

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
        internal bool _isMainModule = false;

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

        public void Error(ModulePosition position, string error)
        {
            Parser.Lexer.Throw(position, error);
        }

        public void Report(ModulePosition position, string error)
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
        internal T NotSupportedType<T>(string type, ModulePosition position)
        {
            return Error<T>(position, $"'{type}' type is not supported yet");
        }

        internal T Error<T>(ModulePosition position, string error)
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
        /// calls the function <see cref="TypeToMugType(MugType, ModulePosition)"/> for each parameter in the past array
        /// </summary>
        internal MugValueType[] ParameterTypesToMugTypes(List<ParameterNode> parameterTypes)
        {
            var result = new MugValueType[parameterTypes.Count];
            for (int i = 0; i < parameterTypes.Count; i++)
                result[i] = parameterTypes[i].Type.ToMugValueType(this);

            return result;
        }

        /// <summary>
        /// calls the function <see cref="TypeToMugType(MugType, ModulePosition)"/> for each parameter in the past array
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

        internal LLVMTypeRef[] MugTypesToLLVMTypes(MugValueType[] parameterTypes)
        {
            var result = new LLVMTypeRef[parameterTypes.Length];
            for (int i = 0; i < parameterTypes.Length; i++)
                result[i] = parameterTypes[i].GetLLVMType(this);

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
            var basetype = errortype.GetEnum().BaseType;

            if (!errortype.IsEnum())
                Error(error.Position, "Expected enum type");
            else if (basetype.Kind != TypeKind.Err)
                Error(error.Position, $"Expected enum with base type 'err', not '{basetype}'");

            return MugValueType.EnumError(new EnumErrorInfo()
            {
                Name = $"{error}!{type}",
                ErrorType = errortype,
                SuccessType = successtype,
                LLVMValue = LLVMTypeRef.CreateStruct(new[] { LLVMTypeRef.Int8, successtype.GetLLVMType(this) }, true)
            });
        }

        internal MugValueType GetBiggestTypeOFVariant(VariantStatement variant)
        {
            var biggestsize = 0;
            MugValueType biggesttype = new();

            foreach (var type in variant.Body)
            {
                var casttype = type.Kind != TypeKind.Pointer ? type.ToMugValueType(this) : MugValueType.Unknown;
                var typesize = casttype.Size(SizeOfPointer, this);
                if (typesize >= biggestsize)
                {
                    biggestsize = typesize;
                    biggesttype = casttype;
                }
            }

            return biggesttype;
        }

        private bool GenerateType(string name, int genericscount)
        {
            foreach (var type in Table.DeclaredTypes)
                if (type.Name == name && type.Generics.Count == genericscount)
                {
                    GenerateType(type);
                    return true;
                }

            return false;
        }

        internal TypeSymbol? GetType(string name, MugValueType[] generics, out string error)
        {
            error = null;
            var symbol = Table.GetType(name, generics);
            if (symbol is not null)
                return symbol;

            if (!GenerateType(name, generics.Length))
            {
                error = $"Undeclared type '{name}'";
                return null;
            }

            return Table.GetType(name, generics);
        }

        internal MugValue? EvaluateStruct(TypeStatement type, MugValueType[] generics, ModulePosition position)
        {
            if (type.Generics.Count != generics.Length)
            {
                Report(position, "Passed wrong number of generics");
                return null;
            }

            PushIllegalType(type.Name);

            var oldGenericParameters = GenericParameters;

            GenericParameters = new();
            AddGenericParameters(type.Generics, generics);

            var fields = new string[type.Body.Count];
            var structModel = new MugValueType[type.Body.Count];
            var fieldPositions = new ModulePosition[type.Body.Count];

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

            var structuretype = MugValueType.Struct($"{type.Name}{(type.Generics.Count > 0 ? $"<{string.Join(", ", generics)}>" : "")}", structModel, fields, fieldPositions, this);
            var structsymbol = MugValue.Struct(Module.AddGlobal(structuretype.GetLLVMType(this), type.Name), structuretype);

            GenericParameters = oldGenericParameters;

            PopIllegalType();

            return structsymbol;
        }

        internal void GenericParametersAdd((string name, MugValueType type) genericParameter, ModulePosition position)
        {
            if (GenericParameters.FindIndex(elem => elem.name == genericParameter.name) != -1)
                Error(position, "Already declared generic parameter");

            GenericParameters.Add(genericParameter);
        }

        internal void ClearGenericParameters()
        {
            GenericParameters.Clear();
        }

        public void DeclareCompilerSymbol(string symbol, bool hasGoodPosition = false, ModulePosition position = new())
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
        internal void ExpectSameTypes(MugValueType firstType, ModulePosition position, string error, params MugValueType[] types)
        {
            for (int i = 0; i < types.Length; i++)
                if (!firstType.Equals(types[i]))
                    Report(position, error);
        }

        internal void ExpectBoolType(MugValueType type, ModulePosition position)
        {
            ExpectSameTypes(type, position, $"Expected 'bool' type, got '{type}'", MugValueType.Bool);
        }

        /// <summary>
        /// same of <see cref="ExpectNonVoidType(LLVMTypeRef, ModulePosition)"/> but tests a <see cref="MugType"/> instead of
        /// a <see cref="LLVMTypeRef"/>
        /// </summary>
        public void ExpectNonVoidType(MugType type, ModulePosition position)
        {
            if (type.Kind == TypeKind.Void)
                Error(position, "In the current context 'void' is not allowed");
        }

        /// <summary>
        /// launches a compilation-error if the type that is passed is of void type:
        /// this function is used in all contexts where the type void is not allowed,
        /// for example in the declaration of variables
        /// </summary>
        internal void ExpectNonVoidType(LLVMTypeRef type, ModulePosition position)
        {
            if (type == LLVMTypeRef.Void)
                Error(position, "Expected a non-void type");
        }

        /// <summary>
        /// check if an id is equal to the id of the entry point and if the parameters are 0,
        /// to allow overload of the main function
        /// </summary>
        internal bool IsEntryPoint(FunctionNode function)
        {
            return
                function.Name == EntryPointName &&
                !function.Base.HasValue &&
                function.Generics.Count == 0 &&
                function.ParameterList.Length == 0;
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
                            type.GetLLVMType(this),
                            MugTypesToLLVMTypes(parameters)));

            // adding a new symbol
            Table.DeclareFunctionSymbol(
                prototype.Name,
                new FunctionSymbol(null, Array.Empty<MugValueType>(), parameters, type, MugValue.From(function, type), prototype.Position),
                prototype.Position, out _);
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

        private void MergeSymbols(ref CompilationUnit unit)
        {
            Table.MergeCompilerSymbols(unit.IRGenerator.Table.CompilerSymbols);
            Table.MergeDefinedSymbols(unit.IRGenerator.Table.DefinedFunctions);
            Table.MergeDeclaredGenericFunctionSymbols(unit.IRGenerator.Table.DeclaredGenericFunctions);
            Table.MergeDeclaredGenericTypesSymbols(unit.IRGenerator.Table.DeclaredGenericTypes);
            Table.MergeDeclaredAsOperatorSymbols(unit.IRGenerator.Table.DefinedAsOperators);
            Table.MergeDefinedEnumTypeSymbols(unit.IRGenerator.Table.DefinedEnumTypes);
            Table.MergeDefinedGenericFunctionSymbols(unit.IRGenerator.Table.DefinedGenericFunctions);
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
                var extensionPosition = new ModulePosition(import.Position.Lexer, (import.Member.Position.Position.Start.Value + path.Value.Length - filekind.Length + 2)..(import.Member.Position.Position.End.Value - 1));

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

        private TokenKind GetValueTokenKindFromType(MugValueTypeKind kind, ModulePosition position)
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
            Token lastOP = default;

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

        internal bool GenerateOverloadsOF(string name, MugValueType[] generics)
        {
            var count = 0;
            foreach (var function in Table.DeclaredFunctions)
                if (function.Name == name)
                {
                    GenerateFunction(function, generics);
                    count++;
                }

            return true;
        }

        private void EmitCompTimeWhen(CompTimeWhenStatement when)
        {
            if (EvaluateCompTimeExprAndGetResult(when.Expression))
                MergeTree((NodeBuilder)when.Body);
        }

        private Token CheckGlobalConstant(INode body, MugType type, ModulePosition position)
        {
            if (body is not Token token)
            {
                Report(position, "Not comptime evaluable");
                return default;
            }

            if (token.Kind == TokenKind.Identifier)
                Report(position, "Place the content of the item here instead");

            checkTypes(token.Kind switch
            {
                TokenKind.ConstantString => TypeKind.String,
                TokenKind.ConstantFloatDigit => TypeKind.Float32,
                TokenKind.ConstantDigit => TypeKind.Int32,
                TokenKind.ConstantChar => TypeKind.Char,
                TokenKind.ConstantBoolean => TypeKind.Bool
            });

            return token;

            void checkTypes(TypeKind gotTypekind)
            {
                if (!type.IsAutomatic() && type.Kind != gotTypekind)
                    Report(position, $"Expected type '{type}'");
            }
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
                    if (function.Generics.Count > 0)
                        Table.DeclareGenericFunction(function);
                    else
                        Table.DeclaredFunctions.Add(function);
                    break;
                case FunctionPrototypeNode prototype:
                    EmitFunctionPrototype(prototype);
                    break;
                case TypeStatement structure:
                    Table.DeclaredTypes.Add(structure);
                    break;
                case VariantStatement variant:
                    Table.DeclareVariant(variant);
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
                case ConstantStatement constant:
                    // Table.DeclareConstant(constant.Name, CheckGlobalConstant(constant.Body, constant.Type), constant.Position);
                    break;
                default:
                    Error(member.Position, "Declaration not supported yet");
                    break;
            }
        }

        internal void AddGenericParameters(List<Token> generics, MugValueType[] genericsInput)
        {
            for (int i = 0; i < generics.Count; i++)
                GenericParametersAdd((generics[i].Value, genericsInput[i]), generics[i].Position);
        }

        internal LLVMValueRef EvaluateFunction(FunctionNode function, LLVMValueRef llvmfunction, MugValueType[] generics)
        {
            var oldgenerics = GenericParameters;

            GenericParameters = new();
            AddGenericParameters(function.Generics, generics);

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

            return llvmfunction;
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
                    function.ReturnType.ToMugValueType(this), GetLLVMPrototype(function, Array.Empty<MugValueType>()),
                    function.Position);

            Table.DeclareAsOperators(functionIdentifier, function.Position, out var index);

            functionIdentifier.Value.LLVMValue = EvaluateFunction(function, functionIdentifier.Value.LLVMValue, Array.Empty<MugValueType>());

            Table.DefinedAsOperators[index] = functionIdentifier;
        }

        internal MugValue GetLLVMPrototype(FunctionNode function, MugValueType[] generics, bool addgenerics = true)
        {
            var oldgenerics = GenericParameters;
            GenericParameters = new();
            if (addgenerics)
                AddGenericParameters(function.Generics, generics);

            var baseoffset = Convert.ToInt32(function.Base.HasValue);

            var paramTypes = new MugValueType[function.ParameterList.Length + baseoffset];
            var retType = function.ReturnType.ToMugValueType(this);

            var types = ParameterTypesToMugTypes(function.ParameterList.Parameters);

            if (function.Base.HasValue)
                paramTypes[0] = function.Base.Value.Type.ToMugValueType(this);

            for (int i = 0; i < types.Length; i++)
                paramTypes[i + baseoffset] = types[i];

            var llvmfunction = Module.AddFunction(function.Name, LLVMTypeRef.CreateFunction(
                retType.GetLLVMType(this),
                MugTypesToLLVMTypes(paramTypes)));

            GenericParameters = oldgenerics;

            return MugValue.From(llvmfunction, retType);
        }

        private void GenerateFunction(FunctionNode function, MugValueType[] generics)
        {
            if (function.Name == AsOperatorOverloading)
            {
                GenerateAsOperator(function);
                return;
            }

            var oldgenerics = GenericParameters;
            GenericParameters = new();
            AddGenericParameters(function.Generics, generics);

            var functionIdentifier = new FunctionSymbol(
                function.Base?.Type.ToMugValueType(this),
                generics,
                ParameterTypesToMugTypes(function.ParameterList.Parameters),
                function.ReturnType.ToMugValueType(this), GetLLVMPrototype(function, generics, false),
                function.Position);

            Table.DeclareFunctionSymbol(function.Name, functionIdentifier, function.Position, out var index);

            functionIdentifier.Value.LLVMValue = EvaluateFunction(function, functionIdentifier.Value.LLVMValue, generics);

            Table.DefinedFunctions[function.Name][index] = functionIdentifier;

            GenericParameters = oldgenerics;
        }

        private void GenerateType(TypeStatement type)
        {
            if (type.Generics.Count > 0)
            {
                Table.DeclareGenericType(type);
                return;
            }

            var generics = Array.Empty<MugValueType>();
            var evaluated = EvaluateStruct(type, generics, type.Position);
            if (!evaluated.HasValue)
                return;

            var typeIdentifier = new TypeSymbol(generics, evaluated.Value, type.Position);

            Table.DeclareType(type.Name, typeIdentifier, type.Position);
        }

        private void GenerateEntrypoint()
        {
            foreach (var function in Table.DeclaredFunctions)
                if (IsEntryPoint(function))
                {
                    GenerateFunction(function, Array.Empty<MugValueType>());
                    return;
                }

            CompilationErrors.Throw("No entrypoint declared");
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

            if (_isMainModule)
                // generate all functions here
                GenerateEntrypoint();

            // checking for errors
            Parser.Lexer.CheckDiagnostic();
        }
    }
}
