using Mug.Compilation;
using Mug.Lexer;
using Mug.Parser;
using Mug.Parser.AST;
using Mug.Parser.AST.Directives;
using Mug.Parser.AST.Statements;
using Mug.Symbols;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.TypeResolution
{
    public class TypeInstaller : CompilerComponent
    {
        public TypeInstaller(CompilationTower tower) : base(tower)
        {
        }

        private void DeclareSymbol(string name, ISymbol symbol)
        {
            Tower.Symbols.SetSymbol(name, symbol);
        }

        private void CheckFunc(ParameterListNode parameters, List<Token> generics, Pragmas pragmas)
        {
            var declared = new string[parameters.Length];
            CheckFunctionParameters(parameters, ref declared);

            declared = new string[generics.Count];
            CheckGenericParameters(generics, ref declared);

            // todo: check pragmas
        }

        private void CheckFunctionParameters(ParameterListNode parameters, ref string[] declared)
        {
            for (var i = 0; i < parameters.Length; i++)
                CheckSingleDeclaration(parameters.Parameters[i].Position, ref declared, i, parameters.Parameters[i].Name, "Parameter");
        }

        private void CheckGenericParameters(List<Token> generics, ref string[] declared)
        {
            for (var i = 0; i < generics.Count; i++)
                CheckSingleDeclaration(generics[i].Position, ref declared, i, generics[i].Value, "Generic parameter");
        }

        private void CheckType(
            List<FunctionStatement> bodyfunctions,
            List<FieldNode> bodyfields,
            List<Token> generics,
            Pragmas pragmas)
        {
            var declared = new string[bodyfunctions.Count];
            CheckTypeMethods(bodyfunctions, ref declared);

            declared = new string[bodyfields.Count];
            CheckTypeFields(bodyfields, ref declared);

            declared = new string[generics.Count];
            CheckGenericParameters(generics, ref declared);

            // todo: check pragmas
        }

        private void CheckTypeFields(List<FieldNode> bodyfields, ref string[] declared)
        {
            for (var i = 0; i < bodyfields.Count; i++)
                CheckSingleDeclaration(bodyfields[i].Position, ref declared, i, bodyfields[i].Name, "Field");
        }

        private void CheckTypeMethods(List<FunctionStatement> bodyfunctions, ref string[] declared)
        {
            for (var i = 0; i < bodyfunctions.Count; i++)
            {
                var function = bodyfunctions[i];
                CheckFunc(function.ParameterList, function.Generics, function.Pragmas);
                CheckSingleDeclaration(bodyfunctions[i].Position, ref declared, i, bodyfunctions[i].Name, "Method");
            }
        }

        private void CheckSingleDeclaration(ModulePosition position, ref string[] declared, int i, string name, string kind)
        {
            if (declared.Contains(name))
                Tower.Symbols.ReportRedeclaration($"{kind} '{name}' is declared multiple times", position);

            declared[i] = name;
        }

        private void DeclareType(TypeStatement type)
        {
            WarnNameIfIsPrimitiveLike(type.Name, type.Position);
            CheckType(type.BodyMethods, type.BodyFields, type.Generics, type.Pragmas);
            DeclareSymbol(type.Name, type);
        }

        private void WarnNameIfIsPrimitiveLike(string name, ModulePosition position)
        {
            if (UnsolvedType.GetTypeKindFromToken(Token.NewInfo(TokenKind.Identifier, name)) is not TypeKind.DefinedType)
                Tower.Warn(position, $"Type '{name}' is automaticaly hidden by the builtin type");
        }

        private void DeclareFunction(FunctionStatement func)
        {
            CheckFunc(func.ParameterList, func.Generics, func.Pragmas);
            DeclareSymbol(func.Name, func);
        }

        private void ProcessImport(ImportDirective directive)
        {
            var pathName = EvaluateImportDirectiveName(directive.Member);
            var path = PathFromLocalOrPackages(directive.Mode, pathName);

            if (!File.Exists(path))
            {
                Tower.Report(pathName.Position, $"Unable to find module '{pathName.Value}'");
                return;
            }

            var moduleName = Path.GetFileNameWithoutExtension(path);
            var unit = new CompilationUnit(null, Path.GetDirectoryName(path), null, path);

            Tower.Diagnostic.AddRange(unit.GenerateIR(out _).Diagnostic);
            var addResponse = Tower.Symbols.ImportedModules.TryAdd(moduleName, unit.Tower.Symbols);
            var detectedCircularImport = CheckForCircolarImport(unit.Tower.Symbols);

            ReportImportErrors(directive, pathName, addResponse, detectedCircularImport);
        }

        private void ReportImportErrors(ImportDirective directive, Token pathName, bool addResponse, bool detectedCircularImport)
        {
            if (!addResponse)
                Tower.Report(directive.Position, $"Module '{pathName.Value}' imported multiple times");
            if (detectedCircularImport)
                Tower.Report(directive.Position, $"Detected circular import: module '{pathName.Value}' imports importer module");
        }

        private bool CheckForCircolarImport(SymbolTable symbols)
        {
            foreach (var moduleImported in symbols.ImportedModules)
                if (Tower.Unit.Paths.Contains(moduleImported.Key))
                    return true;

            return false;
        }

        private string PathFromLocalOrPackages(ImportMode mode, Token pathName)
        {
            return
                mode is ImportMode.FromLocal ?
                    Path.GetFullPath(pathName.Value, Tower.Unit.PathsFolderHead) :
                    ImplementProcessImport();
        }

        private Token EvaluateImportDirectiveName(INode pathName)
        {
            if (pathName is not Token name)
            {
                CompilationTower.Todo($"implement {pathName} in {nameof(EvaluateImportDirectiveName)}");
                return default;
            }

            return name;
        }

        private string ImplementProcessImport()
        {
            CompilationTower.Todo($"implement frompackages in {nameof(ProcessImport)}");
            return null;
        }

        private void RecognizeGlobalStatement(INode global)
        {
            switch (global)
            {
                case TypeStatement statement:
                    DeclareType(statement);
                    break;
                case FunctionStatement statement:
                    DeclareFunction(statement);
                    break;
                case ImportDirective directive:
                    ProcessImport(directive);
                    break;
                default:
                    CompilationTower.Todo($"implement {global} in recognize global statement");
                    break;
            }
        }

        public void Declare()
        {
            foreach (var globalStatement in Tower.AST.Members)
                RecognizeGlobalStatement(globalStatement);
        }
    }
}
