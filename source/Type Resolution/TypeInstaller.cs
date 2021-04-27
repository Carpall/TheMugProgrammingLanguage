using Mug.Compilation;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.AST;
using Mug.Models.Parser.AST.Statements;
using Mug.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.TypeResolution
{
    public class TypeInstaller : MugComponent
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
            int i;

            for (i = 0; i < parameters.Length; i++)
                CheckSingleDeclaration(parameters.Parameters[i].Position, ref declared, i, parameters.Parameters[i].Name, "field");

            declared = new string[generics.Count];

            for (i = 0; i < generics.Count; i++)
                CheckSingleDeclaration(generics[i].Position, ref declared, i, generics[i].Value, "generic parameter");

            // todo: check pragmas
        }

        private void CheckType(List<FieldNode> body, List<Token> generics, Pragmas pragmas)
        {
            var declared = new string[body.Count];
            int i;

            for (i = 0; i < body.Count; i++)
                CheckSingleDeclaration(body[i].Position, ref declared, i, body[i].Name, "field");

            declared = new string[generics.Count];

            for (i = 0; i < generics.Count; i++)
                CheckSingleDeclaration(generics[i].Position, ref declared, i, generics[i].Value, "generic parameter");

            // todo: check pragmas
        }

        private void CheckSingleDeclaration(ModulePosition position, ref string[] declared, int i, string name, string kind)
        {
            if (declared.Contains(name))
                Tower.Report(position, $"'{name}' {kind} is declared multiple times");

            declared[i] = name;
        }

        private void DeclareType(TypeStatement type)
        {
            CheckType(type.Body, type.Generics, type.Pragmas);
            DeclareSymbol(type.Name, new StructSymbol(type));
        }

        private void DeclareFunction(FunctionStatement func)
        {
            CheckFunc(func.ParameterList, func.Generics, func.Pragmas);
            DeclareSymbol(func.Name, new FuncSymbol(func));
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
