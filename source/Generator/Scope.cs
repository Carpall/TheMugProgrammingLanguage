using Mug.Generator.IR;
using Mug.Generator.IR.Builder;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirtualMemory = System.Collections.Generic.Dictionary<string, Mug.Generator.AllocationData>;

namespace Mug.Generator
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
