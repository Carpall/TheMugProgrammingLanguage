using Zap.Models.Generator.IR;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zap.Models.Generator
{
    public record AllocationData(int StackIndex, ZapType Type, bool IsConst);
}
