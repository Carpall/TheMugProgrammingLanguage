using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.AST;
using Mug.Models.Parser.AST.Statements;
using Mug.Symbols;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
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
            // WarnNameIfIsPrimitiveLike(type.Name, type.Position);
            CheckType(type.BodyMethods, type.BodyFields, type.Generics, type.Pragmas);
            DeclareSymbol(type.Name, type);
        }

        private void DeclareFunction(FunctionStatement func)
        {
            CheckFunc(func.ParameterList, func.Generics, func.Pragmas);
            DeclareSymbol(func.Name, func);
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
