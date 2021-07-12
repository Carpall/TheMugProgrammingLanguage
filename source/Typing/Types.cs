using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Typing
{
    public struct TypeType : IType
    {
        public override bool Equals(object obj)
        {
            return obj is TypeType;
        }

        public override string ToString()
        {
            return $"type";
        }
    }

    public struct CharType : IType
    {
        public override bool Equals(object obj)
        {
            return obj is CharType;
        }

        public override string ToString()
        {
            return $"chr";
        }
    }

    public struct AutoType : IType
    {
        public override bool Equals(object obj)
        {
            return obj is AutoType;
        }

        public override string ToString()
        {
            return $"auto";
        }
    }

    public struct VoidType : IType
    {
        public override bool Equals(object obj)
        {
            return obj is VoidType;
        }

        public override string ToString()
        {
            return "void";
        }
    }

    public struct BadType : IType
    {
        public override bool Equals(object obj)
        {
            return obj is BadType;
        }

        public override string ToString()
        {
            return "BadType";
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

        public override bool Equals(object obj)
        {
            return obj is FunctionType fn && ParameterTypes.SequenceEqual(fn.ParameterTypes) && ReturnType.Equals(fn.ReturnType);
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

        public override bool Equals(object obj)
        {
            return obj is IntType intType && Size == intType.Size & IsSigned == intType.IsSigned;
        }

        public override string ToString()
        {
            return $"{(IsSigned ? 'i' : 'u')}{Size}";
        }
    }
}
