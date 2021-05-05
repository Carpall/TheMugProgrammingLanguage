using Zap.Models.Generator.IR;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Models.Generator.IR.Builder
{
    public class MIRModuleBuilder
    {
        private readonly List<MIRFunction> _functions = new();

        public void DefineFunction(MIRFunction function)
        {
            _functions.Add(function);
        }

        public MIR Build()
        {
            return new(_functions.ToArray());
        }
    }
}
