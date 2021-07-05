using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Typing
{
    public struct TypeType : IType
    {
        public override string ToString()
        {
            return $"type";
        }
    }

    public struct CharType : IType
    {
        public override string ToString()
        {
            return $"chr";
        }
    }

    public struct AutoType : IType
    {
        public override string ToString()
        {
            return $"auto";
        }
    }

    public struct VoidType : IType
    {
        public override string ToString()
        {
            return "void";
        }
    }

    public struct FunctionType : IType
    {
        public IType[] ParameterTypes { get; }
        public IType ReturnType { get; }

        public FunctionType(IType[] parameterTypes, IType returnType)
        {
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
        }

        public override string ToString()
        {
            return $"fn({string.Join<IType>(", ", ParameterTypes)}): {ReturnType}";
        }
    }

    public struct IntType : IType
    {
        public int Size { get; }
        public bool IsSigned { get; }

        public IntType(int size, bool isSigned)
        {
            Size = size;
            IsSigned = isSigned;
        }

        public override string ToString()
        {
            return $"{(IsSigned ? 'i' : 'u')}{Size}";
        }
    }
}
