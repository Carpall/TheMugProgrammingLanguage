using Nylon.Models.Generator.IR;
using Nylon.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.Models.Generator.IR.Builder
{
    public class NIRModuleBuilder
    {
        private readonly List<NIRFunction> _functions = new();

        public void DefineFunction(NIRFunction function)
        {
            _functions.Add(function);
        }

        public NIR Build()
        {
            return new(_functions.ToArray());
        }
    }
}
