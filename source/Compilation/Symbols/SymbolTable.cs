using LLVMSharp;
using Mug.Models.Generator;
using Mug.Models.Parser.NodeKinds.Statements;
using Mug.MugValueSystem;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Compilation.Symbols
{
    public struct FunctionSymbol
    {
        public MugValueType ReturnType { get; }
        public MugValue Value { get; }
        public MugValueType? BaseType { get; }
        public MugValueType[] GenericParameters { get; }
        public MugValueType[] Parameters { get; }

        public FunctionSymbol(
            MugValueType? baseType,
            MugValueType[] genericParameters,
            MugValueType[] parameters,
            MugValueType returntype,
            MugValue value)
        {
            BaseType = baseType;
            GenericParameters = genericParameters;
            Parameters = parameters;
            Value = value;
            ReturnType = returntype;
        }

        public override bool Equals(object obj)
        {
            if (obj is not FunctionSymbol id ||
                id.Parameters.Length != Parameters.Length ||
                id.GenericParameters.Length != GenericParameters.Length)
                return false;

            for (int i = 0; i < id.Parameters.Length; i++)
                if (!id.Parameters[i].Equals(Parameters[i]))
                    return false;

            for (int i = 0; i < id.GenericParameters.Length; i++)
                if (!id.GenericParameters[i].Equals(GenericParameters[i]))
                    return false;

            if (id.BaseType.HasValue != BaseType.HasValue)
                return false;

            return !id.BaseType.HasValue || id.BaseType.Value.Equals(BaseType.Value);
        }
    }

    public struct TypeSymbol
    {
        public MugValueType[] GenericParameters { get; }
        public MugValue Value { get; }

        public TypeSymbol(MugValueType[] genericParameters, MugValue value)
        {
            GenericParameters = genericParameters;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is not TypeSymbol id ||
                id.GenericParameters.Length != GenericParameters.Length)
                return false;

            for (int i = 0; i < id.GenericParameters.Length; i++)
                if (!id.GenericParameters[i].Equals(GenericParameters[i]))
                    return false;

            return true;
        }
    }

    public class SymbolTable
    {
        private readonly IRGenerator _generator;

        // prototypes
        public readonly List<FunctionNode> DeclaredFunctions = new();
        public readonly List<TypeStatement> DeclaredTypes = new();

        // implementations
        public readonly Dictionary<string, List<FunctionSymbol>> DefinedFunctions = new();
        public readonly Dictionary<string, List<TypeSymbol>> DefinedTypes = new();

        // generic prototypes
        public readonly List<TypeStatement> DefinedGenericTypes = new();
        public readonly List<FunctionNode> DefinedGenericFunctions = new();

        // compiler symbols like flags
        public readonly List<string> CompilerSymbols = new();

        public SymbolTable(IRGenerator generator)
        {
            _generator = generator;
        }

        public void DeclareFunctionSymbol(string name, FunctionSymbol identifier, Range position)
        {
            if (!DefinedFunctions.TryAdd(name, new() { identifier }))
            {
                if (DefinedFunctions[name].FindIndex(id => id.Equals(identifier)) != -1)
                {
                    _generator.Report(position, $"Function '{name}' already declared");
                    return;
                }

                DefinedFunctions[name].Add(identifier);
            }
        }

        public void DeclareType(string name, TypeSymbol identifier, Range position)
        {
            if (!DefinedTypes.TryAdd(name, new() { identifier }))
            {
                if (DefinedTypes[name].FindIndex(id => id.Equals(identifier)) != -1)
                {
                    _generator.Report(position, $"Type '{name}' already declared");
                    return;
                }

                DefinedTypes[name].Add(identifier);
            }
        }

        public void DeclareGenericFunction(FunctionNode function)
        {
            var symbol = DefinedGenericFunctions.Find(t =>
            {
                return t.Name == function.Name && t.Generics.Count == function.Generics.Count;
            });

            if (symbol is not null)
            {
                _generator.Report(function.Position, $"A generic function named '{function.Name}', which accepts {function.Generics.Count} generic parameters, is already declared");
                return;
            }

            DefinedGenericFunctions.Add(function);
        }

        public void DeclareGenericType(TypeStatement type)
        {
            var symbol = DefinedGenericTypes.Find(t => t.Name == type.Name && t.Generics.Count == type.Generics.Count);
            if (symbol is not null)
            {
                _generator.Report(type.Position, $"A generic type named '{type.Name}', which accepts {type.Generics.Count} generic parameters, is already declared");
                return;
            }

            DefinedGenericTypes.Add(type);
        }

        public bool DeclareCompilerSymbol(string name, Range position)
        {
            if (CompilerSymbols.Contains(name))
            {
                _generator.Report(position, "Already declared compiler symbol");
                return false;
            }

            CompilerSymbols.Add(name);
            return true;
        }

        public void DefineFunctionSymbol(string name, int index, FunctionSymbol definition)
        {
            DefinedFunctions[name][index] = definition;
        }

        public TypeSymbol? GetType(string name, MugValueType[] generics, out string error)
        {
            if (!DefinedTypes.TryGetValue(name, out var overloads))
            {
                error = $"Undeclared type '{name}'";
                return null;
            }

            var index = overloads.FindIndex(id =>
            {
                for (int i = 0; i < id.GenericParameters.Length; i++)
                    if (!id.GenericParameters[i].Equals(generics[i]))
                        return false;

                return true;
            });

            if (index == -1)
            {
                error = $"No type '{name}' accepts {generics.Length} generic parameters";
                return null;
            }

            error = null;
            return overloads[index];
        }

        public TypeStatement GetGenericType(string name, int genericsCount, Range position)
        {
            var index = DefinedGenericTypes.FindIndex(id =>
            {
                return id.Name == name && id.Generics.Count == genericsCount;
            });

            if (index == -1)
            {
                _generator.Report(position, $"No generic type '{name}' accepts {genericsCount} generic parameter{IRGenerator.GetPlural(genericsCount)}");
                return null;
            }

            return DefinedGenericTypes[index];
        }

        public bool CompilerSymbolIsDeclared(string name)
        {
            return CompilerSymbols.Contains(name);
        }

        public TypeSymbol GetType(string enumname, Range position)
        {
            // tofix
            throw new();
        }
    }
}
