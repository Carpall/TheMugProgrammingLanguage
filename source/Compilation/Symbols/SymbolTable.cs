using LLVMSharp.Interop;
using Mug.Models.Generator;
using Mug.Models.Lexer;
using Mug.Models.Parser.NodeKinds.Statements;
using Mug.MugValueSystem;
using System.Collections.Generic;
using System.Linq;

namespace Mug.Compilation.Symbols
{
  public class SymbolTable
    {
        private readonly IRGenerator _generator;

        // prototypes
        public readonly List<FunctionPrototypeNode> DeclaredFunctionPrototypes = new();
        public readonly List<FunctionNode> DeclaredFunctions = new();
        public readonly List<TypeStatement> DeclaredTypes = new();
        public readonly List<VariantStatement> DeclaredVariants = new();

        // generic prototypes
        public readonly List<TypeStatement> DeclaredGenericTypes = new();
        public readonly List<FunctionNode> DeclaredGenericFunctions = new();

        // implementations
        public readonly Dictionary<string, List<FunctionSymbol>> DefinedFunctions = new();
        public readonly List<FunctionSymbol> DefinedAsOperators = new();
        public readonly Dictionary<string, List<FunctionSymbol>> DefinedGenericFunctions = new();
        public readonly Dictionary<string, List<TypeSymbol>> DefinedTypes = new();
        public readonly Dictionary<string, (MugValue model, ModulePosition position)> DefinedEnumTypes = new();
        public readonly List<(string name, Token value)> DefinedConstants = new();

        // compiler symbols like flags
        public readonly List<string> CompilerSymbols = new();
        public readonly Dictionary<string, LLVMValueRef> ConstStringCache = new();

        public SymbolTable(IRGenerator generator)
        {
            _generator = generator;
        }

        public void DeclareFunctionSymbol(string name, FunctionSymbol identifier, ModulePosition position, out int index)
        {
            index = 0;
            if (DefinedFunctions.TryAdd(name, new() { identifier }))
                return;

            if (DefinedFunctions[name].FindIndex(id => id.Equals(identifier)) != -1)
            {
                _generator.Report(position, $"Function '{name}' already declared");
                return;
            }

            index = DefinedFunctions[name].Count;
            DefinedFunctions[name].Add(identifier);
        }

        public void DeclareGenericFunctionSymbol(string name, FunctionSymbol identifier)
        {
            if (DefinedGenericFunctions.TryAdd(name, new() { identifier }))
                return;

            if (DefinedGenericFunctions[name].FindIndex(id => id.Equals(identifier)) != -1)
                return;

            DefinedGenericFunctions[name].Add(identifier);
        }

        public void DeclareType(string name, TypeSymbol identifier, ModulePosition position)
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

        public void DeclareEnumType(string name, MugValue enumtype, ModulePosition position)
        {
            if (!DefinedEnumTypes.TryAdd(name, (enumtype, position)))
            {
                _generator.Report(position, $"Enum type '{name}' already declared");
                return;
            }
        }

        public void DeclareGenericFunction(FunctionNode function)
        {
            var functionstring = function.ToString();
            
            var symbol = DeclaredGenericFunctions.Find(t =>
            {
                return t.ToString() == functionstring;
            });

            if (symbol is not null)
            {
                _generator.Report(function.Position, $"A generic function named '{function.Name}', which accepts {function.Generics.Count} generic parameter{IRGenerator.GetPlural(function.Generics.Count)}, is already declared");
                return;
            }

            DeclaredGenericFunctions.Add(function);
        }

        public void DeclareVariant(VariantStatement variant)
        {
            if (DeclaredVariants.FindIndex(v => v.Name == variant.Name) != -1)
                _generator.Report(variant.Position, $"Variant type '{variant.Name}' is already declared");
            else if (DeclaredTypes.FindIndex(t => t.Name == variant.Name) != -1)
                _generator.Report(variant.Position, $"Variant type '{variant.Name}' hides another non-variant type");
            else
                DeclaredVariants.Add(variant);
        }

        public List<FunctionNode> GetOverloadsOFGenericFunction(string name)
        {
            var result = new List<FunctionNode>();
            foreach (var genericFunction in DeclaredGenericFunctions)
                if (genericFunction.Name == name)
                    result.Add(genericFunction);

            return result;
        }

        public bool GetGenericFunctionSymbol(string name, MugValueType? basetype, MugValueType[] parameters, MugValueType returntype, out FunctionSymbol identifier)
        {
            identifier = new();

            if (!DefinedGenericFunctions.TryGetValue(name, out var overloads))
                return false;

            for (int i = 0; i < overloads.Count; i++)
            {
                var function = overloads[i];
                if (!overloads[i].BaseType.Equals(basetype) || parameters.Length != function.Parameters.Length || !returntype.Equals(function.ReturnType))
                    continue;

                for (int j = 0; j < parameters.Length; j++)
                    if (!parameters[j].Equals(function.Parameters[j]))
                        goto unequalContinue;

                identifier = function;
                return true;
            unequalContinue:;
            }

            return false;
        }

        public bool IsAVariantType(string name, out MugValueType? variant)
        {
            // todo: cache variant time for next use
            variant = null;
            var index = DeclaredVariants.FindIndex(v => v.Name == name);
            if (index != -1)
                variant = MugValueType.Variant(DeclaredVariants[index]);

            return index != -1;
        }

        public MugValue? GetEnumType(string name, ModulePosition position, bool report)
        {
            if (!DefinedEnumTypes.TryGetValue(name, out var enumtype))
            {
                if (report)
                    _generator.Report(position, $"Undeclared enum type '{name}'");

                return null;
            }

            return enumtype.model;
        }

        public void DeclareGenericType(TypeStatement type)
        {
            var symbol = DeclaredGenericTypes.Find(t => t.Name == type.Name && t.Generics.Count == type.Generics.Count);
            if (symbol is not null)
            {
                _generator.Report(type.Position, $"A generic type named '{type.Name}', which accepts {type.Generics.Count} generic parameters, is already declared");
                return;
            }

            DeclaredGenericTypes.Add(type);
        }

        public bool DeclareCompilerSymbol(string name, ModulePosition position)
        {
            if (CompilerSymbols.Contains(name))
            {
                _generator.Report(position, "Already declared compiler symbol");
                return false;
            }

            CompilerSymbols.Add(name);
            return true;
        }

        public TypeSymbol? GetType(string name, MugValueType[] generics)
        {
            if (!DefinedTypes.TryGetValue(name, out var overloads))
                return null;

            var index = overloads.FindIndex(id =>
            {
                for (int i = 0; i < id.GenericParameters.Length; i++)
                    if (!id.GenericParameters[i].Equals(generics[i]))
                        return false;

                return true;
            });

            if (index == -1)
                return null;

            return overloads[index];
        }

        public TypeStatement GetGenericType(string name, int genericsCount, ModulePosition position)
        {
            var index = DeclaredGenericTypes.FindIndex(id =>
            {
                return id.Name == name && id.Generics.Count == genericsCount;
            });

            if (index == -1)
            {
                _generator.Report(position, $"No generic type '{name}' accepts {genericsCount} generic parameter{IRGenerator.GetPlural(genericsCount)}");
                return null;
            }

            return DeclaredGenericTypes[index];
        }

        public bool CompilerSymbolIsDeclared(string name)
        {
            return CompilerSymbols.Contains(name);
        }

        public void DeclareAsOperators(FunctionSymbol function, ModulePosition position, out int index)
        {
            index = 0;
            if (DefinedAsOperators.FindIndex(symbol =>
            {
                return symbol.Parameters[0].Equals(function.Parameters[0]) && symbol.ReturnType.Equals(function.ReturnType);
            }) != -1)
            {
                _generator.Report(position, $"'as' operator which casts '{function.Parameters.First()}' to '{function.ReturnType}' is already declared");
                return;
            }

            index = DefinedAsOperators.Count;
            DefinedAsOperators.Add(function);
        }

        public FunctionSymbol? GetAsOperator(MugValueType type, MugValueType returntype, ModulePosition position)
        {
            var index = DefinedAsOperators.FindIndex(function => function.Parameters[0].Equals(type) && function.ReturnType.Equals(returntype));
            if (index == -1)
            {
                _generator.Report(position, $"'as' operator which casts '{type}' to '{returntype}' is not declared");
                return null;
            }

            return DefinedAsOperators[index];
        }

        public void MergeCompilerSymbols(List<string> compilersymbols)
        {
            foreach (var symbol in compilersymbols)
                CompilerSymbols.Add(symbol);
        }

        public void MergeDefinedFunctionSymbols(Dictionary<string, List<FunctionSymbol>> functions)
        {
            foreach (var overloads in functions)
                foreach (var function in overloads.Value)
                    DeclareFunctionSymbol(overloads.Key, function, function.Position, out _);
        }

        public void MergeDeclaredFunctionSymbols(List<FunctionNode> functions)
        {
            for (int i = 0; i < functions.Count; i++)
            {
                var function = functions[i];
                DeclaredFunctions.Add(function);
            }
        }

        public void MergeDeclaredTypeSymbols(List<TypeStatement> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                DeclaredTypes.Add(type);
            }
        }

        public void MergeDeclaredGenericFunctionSymbols(List<FunctionNode> genericFunctions)
        {
            for (int i = 0; i < genericFunctions.Count; i++)
            {
                var function = genericFunctions[i];
                DeclareGenericFunction(function);
            }
        }

        public void MergeDeclaredGenericTypesSymbols(List<TypeStatement> genericTypes)
        {
            for (int i = 0; i < genericTypes.Count; i++)
            {
                var type = genericTypes[i];
                DeclareGenericType(type);
            }
        }

        public void MergeDeclaredAsOperatorSymbols(List<FunctionSymbol> asOperators)
        {
            foreach (var asoperator in asOperators)
                DeclareAsOperators(asoperator, asoperator.Position, out _);
        }

        public void MergeDefinedEnumTypeSymbols(Dictionary<string, (MugValue model, ModulePosition position)> enumTypes)
        {
            foreach (var enumtype in enumTypes)
                DeclareEnumType(enumtype.Key, enumtype.Value.model, enumtype.Value.position);
        }

        public void MergeDefinedGenericFunctionSymbols(Dictionary<string, List<FunctionSymbol>> genericFunctions)
        {
            foreach (var overloads in genericFunctions)
                foreach (var function in overloads.Value)
                    DeclareGenericFunctionSymbol(overloads.Key, function);
        }

        public void DeclareConstant(string name, Token value, ModulePosition position)
        {
            if (DefinedConstants.FindIndex(c => c.name == name) != -1)
            {
                _generator.Report(position, $"Global const '{name}' is already declared");
                return;
            }

            DefinedConstants.Add((name, value));
        }
    }
}
