using Mug.Compilation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.IR
{
    public struct MIR
    {
        public MIRGlobal[] Globals { get; }
        public MIRFunction[] Functions { get; }
        public MIRFunctionPrototype[] FunctionPrototypes { get; }
        public MIRStructure[] Structures { get; }

        public MIR(MIRGlobal[] globals, MIRFunction[] functions, MIRFunctionPrototype[] functionPrototypes, MIRStructure[] structures)
        {
            Globals = globals;
            Functions = functions;
            FunctionPrototypes = functionPrototypes;
            Structures = structures;
        }

        public string DumpJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string Dump()
        {
            var globals = Globals ?? Array.Empty<MIRGlobal>();
            var functionPrototypes = FunctionPrototypes ?? Array.Empty<MIRFunctionPrototype>();
            var functions = Functions ?? Array.Empty<MIRFunction>();
            var structures = Structures ?? Array.Empty<MIRStructure>();

            return $"{string.Join("\n", globals)}\n{string.Join("\n", structures)}\n{string.Join("\n", functionPrototypes)}\n\n{string.Join("\n", functions)}";
        }

        public override string ToString()
        {
            return Dump();
        }

        public MIRFunctionPrototype GetFunction(string functionName, out bool isExtern)
        {
            isExtern = false;
            for (int i = 0; i < Functions.Length; i++)
                if (Functions[i].Prototype.Name == functionName)
                    return Functions[i].Prototype;

            isExtern = true;
            return GetFunctionPrototype(functionName);
        }

        private MIRFunctionPrototype GetFunctionPrototype(string functionName)
        {
            for (int i = 0; i < FunctionPrototypes.Length; i++)
                if (FunctionPrototypes[i].Name == functionName)
                    return FunctionPrototypes[i];

            CompilationTower.Throw($"funciton '{functionName}' is not declared");
            return default;
        }
    }
}
