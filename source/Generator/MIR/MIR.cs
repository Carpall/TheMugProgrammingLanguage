using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Models.Generator.IR
{
    public struct MIR
    {
        public MIRFunction[] Functions { get; }
        public MIRStructure[] Structures { get; }

        public MIR(MIRFunction[] functions, MIRStructure[] structures)
        {
            Functions = functions;
            Structures = structures;
        }

        public string DumpJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string Dump()
        {
            var functions = Functions ?? Array.Empty<MIRFunction>();
            var structures = Structures ?? Array.Empty<MIRStructure>();

            return $"{string.Join("\n", structures)}\n{string.Join("\n", functions)}";
        }

        public override string ToString()
        {
            return Dump();
        }
    }
}
