using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR.Values.Typing
{
    public struct Untyped : ILiquorType
    {
        public override string ToString()
        {
            return "Untyped";
        }
    }
}
