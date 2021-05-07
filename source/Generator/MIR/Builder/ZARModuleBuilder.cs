using Zap.Models.Generator.IR;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Models.Generator.IR.Builder
{
    public class ZARModuleBuilder
    {
        private readonly List<ZARFunction> _functions = new();

        public void DefineFunction(ZARFunction function)
        {
            _functions.Add(function);
        }

        public ZAR Build()
        {
            return new(_functions.ToArray());
        }
    }
}
