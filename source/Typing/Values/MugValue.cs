using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Typing.Values
{
    public readonly struct MugValue
    {
        public readonly IType Type;

        public readonly object ConstantValue;

        public readonly bool IsConst;

        public readonly bool IsValid;

        public readonly bool IsSpecial() => Type is TypeType;

        internal MugValue(IType type, object constantValue, bool isConst, bool isValid = true)
        {
            Type = type;
            ConstantValue = constantValue;
            IsConst = isConst;
            IsValid = isValid;
        }

        internal static MugValue Invalid => new(IType.BadType, null, false, false);
    }
}
