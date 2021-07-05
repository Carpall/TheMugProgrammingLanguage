using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Typing.Values
{
    public class MugValue
    {
        public IType Type { get; }

        public object ConstantValue { get; }

        public bool IsConst { get; }

        public MugValue(IType type, object constantValue, bool isConst)
        {
            Type = type;
            ConstantValue = constantValue;
            IsConst = isConst;
        }

        internal static MugValue BadValue => new(IType.BadType, null, false);
    }
}
