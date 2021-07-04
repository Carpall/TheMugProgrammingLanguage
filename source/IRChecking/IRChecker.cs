using Mug.AstGeneration.IR;
using Mug.AstGeneration.IR.Values;
using Mug.AstGeneration.IR.Values.Instructions;
using Mug.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.IRChecking
{
    public class IRChecker : CompilerComponent
    {
        private const string EntryPointName = "main";

        private LiquorIR IR { get; set; }
        
        private LiquorBlock CurrentBlock { get; set; }

        public ScopeMemory Memory { get; set; }

        public IRChecker(CompilationInstance tower) : base(tower)
        {
        }

        public void SetIR(LiquorIR ir)
        {
            IR = ir;
        }

        public LiquorIR Check()
        {
            AnalyzeFunction(GetEntryPointComptimeVariable());
            return IR;
        }

        private LiquorComptimeVariable GetEntryPointComptimeVariable()
        {
            foreach (var declaration in IR.Declarations)
                if (declaration.Name == EntryPointName)
                    return declaration;

            Tower.Throw(Tower.Sources.First(), 0, "Missing entrypoint");
            return default;
        }

        private void CheckEntryPoint(LiquorComptimeVariable declaration)
        {
            throw new NotImplementedException();
        }

        private void AnalyzeBlock(LiquorBlock block)
        {
            var oldBlock = SaveCurrentBlock(block);

            foreach (var instruction in CurrentBlock.Instructions)
                AnalyzeInstruction(instruction);

            RestoreBlock(oldBlock);
        }

        private void AnalyzeInstruction(ILiquorValue instruction)
        {
            switch (instruction)
            {
                case AllocaInst inst:
                    DeclareAllocation(inst);
                    break;
                case LoadLocalInst inst:
                    MaybeReportIFNotDeclared(inst.Name, inst.Position);
                    break;
                case LoadLocalAddressInst inst:
                    MaybeReportIFNotDeclared(inst.Name, inst.Position);
                    break;
                case StoreLocalInst inst:
                    MaybeReportIFNotDeclared(inst.Name, inst.Position);
                    break;
                default:
                    break;
            }
        }

        private void DeclareAllocation(AllocaInst alloca)
        {
            if (Memory.IsDeclared(alloca.Name, out _))
                Tower.Report(alloca.Position, $"Declared multiple times variable '{alloca.Name}'");
            
            if (alloca.IsAssigned)
            {
                var body = CollectBodyUntil<StoreLocalInst>();
                var type = GetType(body);

                Memory.Declare(alloca.Name, alloca);
            }
        }

        private  CollectBodyUntil<T>()
        {

        }

        private void MaybeReportIFNotDeclared(string name, ModulePosition position)
        {
            if (!Memory.IsDeclared(name, out _))
                Tower.Report(position, $"Undeclared variable '{name}'");
        }

        private void RestoreBlock(LiquorBlock oldBlock)
        {
            CurrentBlock = oldBlock;
        }

        private LiquorBlock SaveCurrentBlock(LiquorBlock block)
        {
            var result = CurrentBlock;
            CurrentBlock = block;
            return result;
        }

        private void AnalyzeFunction(LiquorComptimeVariable variable)
        {
            AnalyzeBlock(variable.Body);
        }
    }
}
