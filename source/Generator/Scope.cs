using Mug.Models.Generator.IR;
using Mug.Models.Generator.IR.Builder;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirtualMemory = System.Collections.Generic.Dictionary<string, Mug.Models.Generator.AllocationData>;

namespace Mug.Models.Generator
{
    struct Scope
    {
        internal DataType Type { get; set; }
        internal bool IsInFunctionBlock { get; }
        internal VirtualMemory VirtualMemory { get; }

        public Scope(DataType type, bool isinfunctionblock, VirtualMemory allocations)
        {
            Type = type;
            IsInFunctionBlock = isinfunctionblock;
            VirtualMemory = new(allocations);
        }
    }
}
