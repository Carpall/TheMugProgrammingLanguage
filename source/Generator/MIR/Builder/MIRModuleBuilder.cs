using Mug.Generator.IR;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR.Builder
{
    public class MIRModuleBuilder
    {
        private readonly List<MIRFunction> _functions = new();
        private readonly List<MIRStructure> _structures = new();

        public void DefineFunction(MIRFunction function)
        {
            _functions.Add(function);
        }

        public MIR Build()
        {
            return new(_functions.ToArray(), _structures.ToArray());
        }

        internal void DefineStruct(MIRStructure structure)
        {
            _structures.Add(structure);
        }
    }
}
