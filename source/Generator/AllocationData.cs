using Mug.Models.Generator.IR;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Models.Generator
{
    public record AllocationData(int StackIndex, MugType Type, bool IsConst);
}
