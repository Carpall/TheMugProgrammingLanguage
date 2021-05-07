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
        internal ZARFunctionBuilder Parent { get; set; }
        internal AllocationData HiddenAllocationBuffer { get; set; }
        internal bool IsInFunctionBlock { get; }

        public Scope(ZARFunctionBuilder functionbuilder, AllocationData hiddenAllocationBuffer, bool isinfunctionblock)
        {
            Parent = functionbuilder;
            HiddenAllocationBuffer = hiddenAllocationBuffer;
            IsInFunctionBlock = isinfunctionblock;
        }
    }
}
