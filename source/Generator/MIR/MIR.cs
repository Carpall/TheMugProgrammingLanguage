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
        public MIRFunction[] Functions { get; }
        public int ByteSize => Marshal.SizeOf(this);

        public string Dump()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
