using Zap.Models.Generator.IR;
using Zap.Models.Generator.IR.Builder;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirtualMemory = System.Collections.Generic.Dictionary<string, Zap.Models.Generator.AllocationData>;

namespace Zap.Models.Generator
{
    struct Scope
    {
        internal AllocationData HiddenAllocationBuffer { get; set; }
        internal bool IsInFunctionBlock { get; }
        internal VirtualMemory VirtualMemory { get; }

        public Scope(AllocationData hiddenAllocationBuffer, bool isinfunctionblock, VirtualMemory allocations)
        {
            HiddenAllocationBuffer = hiddenAllocationBuffer;
            IsInFunctionBlock = isinfunctionblock;
            VirtualMemory = new(allocations);
        }
    }
}
