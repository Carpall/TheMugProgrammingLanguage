using Mug.MugValueSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mug.Compilation.Symbols
{
    public struct TypeSymbol
    {
        public MugValueType[] GenericParameters { get; }
        public MugValue Value { get; }
        public ModulePosition Position { get; }

        public TypeSymbol(MugValueType[] genericParameters, MugValue value, ModulePosition position)
        {
            GenericParameters = genericParameters;
            Value = value;
            Position = position;
        }

        public override bool Equals(object obj)
        {
            if (obj is not TypeSymbol id ||
                id.GenericParameters.Length != GenericParameters.Length)
                return false;

            for (int i = 0; i < id.GenericParameters.Length; i++)
                if (!id.GenericParameters[i].Equals(GenericParameters[i]))
                    return false;

            return true;
        }
    }
}
