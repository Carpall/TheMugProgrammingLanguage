using Zap.Models.Generator.IR;
using Zap.Models.Generator.IR.Builder;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Models.Generator
{
    struct Scope
    {
        internal AllocationData HiddenAllocationBuffer { get; set; }
        internal bool IsInFunctionBlock { get; }

        public Scope(AllocationData hiddenAllocationBuffer, bool isinfunctionblock)
        {
            HiddenAllocationBuffer = hiddenAllocationBuffer;
            IsInFunctionBlock = isinfunctionblock;
        }
    }
}
