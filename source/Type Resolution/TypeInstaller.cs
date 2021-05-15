using Nylon.Compilation;
using Nylon.Models.Lexer;
using Nylon.Models.Parser;
using Nylon.Models.Parser.AST;
using Nylon.Models.Parser.AST.Statements;
using Nylon.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.TypeResolution
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
            
            for (int i = 0; i < parameters.Length; i++)
                CheckSingleDeclaration(parameters.Parameters[i].Position, ref declared, i, parameters.Parameters[i].Name, "Parameter");

            declared = new string[generics.Count];
            CheckGenericParameters(generics, ref declared);

            // todo: check pragmas
        }

        private void CheckGenericParameters(List<Token> generics, ref string[] declared)
        {
            for (int i = 0; i < generics.Count; i++)
                CheckSingleDeclaration(generics[i].Position, ref declared, i, generics[i].Value, "Generic parameter");
        }

        private void CheckType(List<FunctionStatement> bodyfunctions, List<FieldNode> bodyfields, List<Token> generics, Pragmas pragmas)
        {
            var declared = new string[bodyfunctions.Count];

            for (int i = 0; i < bodyfunctions.Count; i++)
            {
                var function = bodyfunctions[i];
                CheckFunc(function.ParameterList, function.Generics, function.Pragmas);
                CheckSingleDeclaration(bodyfunctions[i].Position, ref declared, i, bodyfunctions[i].Name, "Method");
            }

            declared = new string[bodyfields.Count];

            for (int i = 0; i < bodyfields.Count; i++)
                CheckSingleDeclaration(bodyfields[i].Position, ref declared, i, bodyfields[i].Name, "Field");

            declared = new string[generics.Count];

            CheckGenericParameters(generics, ref declared);

            // todo: check pragmas
        }

        private void CheckSingleDeclaration(ModulePosition position, ref string[] declared, int i, string name, string kind)
        {
            if (declared.Contains(name))
                Tower.Report(position, $"{kind} '{name}' is declared multiple times");

            declared[i] = name;
        }

        private void DeclareType(TypeStatement type)
        {
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
