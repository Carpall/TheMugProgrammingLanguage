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

        public MIR(MIRFunction[] functions)
        {
            Functions = functions;
        }

        public string Dump()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
