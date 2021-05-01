using Mug.Models.Generator.IR;
using Mug.Models.Generator.IR.Builder;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Models.Generator
{
    struct Scope
    {
        internal MIRFunctionBuilder Parent { get; set; }
        internal AllocationData HiddenAllocationBuffer { get; set; }
        internal bool IsInFunctionBlock { get; }

        public Scope(MIRFunctionBuilder functionbuilder, AllocationData hiddenAllocationBuffer, bool isinfunctionblock)
        {
            Parent = functionbuilder;
            HiddenAllocationBuffer = hiddenAllocationBuffer;
            IsInFunctionBlock = isinfunctionblock;
        }
    }
}
