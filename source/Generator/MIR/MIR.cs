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
        public MIRStructure[] Structures { get; }

        public MIR(MIRGlobal[] globals, MIRFunction[] functions, MIRStructure[] structures)
        {
            Globals = globals;
            Functions = functions;
            Structures = structures;
        }

        public string DumpJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string Dump()
        {
            var globals = Globals ?? Array.Empty<MIRGlobal>();
            var functions = Functions ?? Array.Empty<MIRFunction>();
            var structures = Structures ?? Array.Empty<MIRStructure>();

            return $"{string.Join("\n", globals)}\n{string.Join("\n", structures)}\n{string.Join("\n", functions)}";
        }

        public override string ToString()
        {
            return Dump();
        }

        public MIRFunction GetFunction(string functionName)
        {
            for (int i = 0; i < Functions.Length; i++)
                if (Functions[i].Name == functionName)
                    return Functions[i];

            return default;
        }
    }
}
