using Mug.Models.Generator.IR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Models.Generator.IR.Builder
{
    internal class MIRModuleBuilder
    {
        private readonly List<MIRFunction> _functions = new();

        public void Define(MIRFunction function)
        {
            _functions.Add(function);
        }

        public MIR Build()
        {
            return new(_functions.ToArray());
        }
    }
}
