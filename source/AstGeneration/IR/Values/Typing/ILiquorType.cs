using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR.Values.Typing
{
    public interface ILiquorType
    {
        internal static ILiquorType Untyped => new Untyped();

        internal static ILiquorType Int8 => null;
    }
}
