using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nylon.Models.Generator.IR
{
    public struct NIR
    {
        public NIRFunction[] Functions { get; }

        public NIR(NIRFunction[] functions)
        {
            Functions = functions;
        }

        public string DumpJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string Dump()
        {
            return $"{string.Join("\n", Functions)}";
        }
    }
}
