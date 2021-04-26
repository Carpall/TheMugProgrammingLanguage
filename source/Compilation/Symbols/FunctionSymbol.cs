using Mug.MugValueSystem;

namespace Mug.Compilation.Symbols
{
  public struct FunctionSymbol
    {
        public MugValueType ReturnType { get; }
        public MugValue Value;
        public MugValueType? BaseType { get; }
        public MugValueType[] GenericParameters { get; }
        public MugValueType[] Parameters { get; }
        public ModulePosition Position { get; }

        public FunctionSymbol(
            MugValueType? baseType,
            MugValueType[] genericParameters,
            MugValueType[] parameters,
            MugValueType returntype,
            MugValue value,
            ModulePosition position)
        {
            BaseType = baseType;
            GenericParameters = genericParameters;
            Parameters = parameters;
            Value = value;
            ReturnType = returntype;
            Position = position;
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
