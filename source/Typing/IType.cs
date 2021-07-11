using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Typing
{
    public interface IType
    {
        internal static BadType BadType => new();

        internal static IntType Int(int size, bool isSigned) => new(size, isSigned);

        internal static FunctionType Fn(IType[] parameterTypes, IType returnType) => new(parameterTypes, returnType);

        internal static VoidType Void => new();

        internal static AutoType Auto => new();

        internal static CharType Char => new();

        internal static TypeType Type => new();

        abstract public string ToString();
    }
}
