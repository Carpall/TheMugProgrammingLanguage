using Mug.MugValueSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Compilation.Symbols
{
    public struct FunctionSymbol
    {
        public MugValueType ReturnType { get; }
        public MugValue Value { get; set; }
        public MugValueType? BaseType { get; }
        public MugValueType[] GenericParameters { get; }
        public MugValueType[] Parameters { get; }

        public FunctionSymbol(
            MugValueType? baseType,
            MugValueType[] genericParameters,
            MugValueType[] parameters,
            MugValueType returntype,
            MugValue value)
        {
            BaseType = baseType;
            GenericParameters = genericParameters;
            Parameters = parameters;
            Value = value;
            ReturnType = returntype;
        }

        public override bool Equals(object obj)
        {
            if (obj is not FunctionSymbol id ||
                id.Parameters.Length != Parameters.Length ||
                id.GenericParameters.Length != GenericParameters.Length)
                return false;

            for (int i = 0; i < id.Parameters.Length; i++)
                if (!id.Parameters[i].Equals(Parameters[i]))
                    return false;

            for (int i = 0; i < id.GenericParameters.Length; i++)
                if (!id.GenericParameters[i].Equals(GenericParameters[i]))
                    return false;

            if (id.BaseType.HasValue != BaseType.HasValue)
                return false;

            return !id.BaseType.HasValue || id.BaseType.Value.Equals(BaseType.Value);
        }
    }
}
