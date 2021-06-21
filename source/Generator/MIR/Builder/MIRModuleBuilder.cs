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
        private readonly List<MIRGlobal> _globals = new();
        private readonly List<MIRStructure> _structures = new();
        private readonly List<MIRFunction> _functions = new();
        private readonly List<MIRFunctionPrototype> _functionPrototypes = new();

        public void DefineFunction(MIRFunction function)
        {
            _functions.Add(function);
        }

        public MIR Build()
        {
            return new(_globals.ToArray(), _functions.ToArray(), _functionPrototypes.ToArray(), _structures.ToArray());
        }

        public void DefineStruct(MIRStructure structure)
        {
            _structures.Add(structure);
        }

        public void DefineGlobal(MIRGlobal name)
        {
            _globals.Add(name);
        }

        public bool FunctionPrototypeIsDeclared(string name)
        {
            return _functionPrototypes.FindIndex(functionPrototype => functionPrototype.Name == name) != -1;
        }

        public void DefineFunctionPrototype(string name, DataType type, DataType[] parameterTypes)
        {
            _functionPrototypes.Add(new(name, type, parameterTypes));
        }
    }
}
