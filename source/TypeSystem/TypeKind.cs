namespace Mug.TypeSystem
{
  public enum TypeKind
    {
        // implicit type specification in var, const declarations
        Auto,
        Pointer,
        String,
        Char,
        Int32,
        Int64,
        UInt8,
        UInt32,
        UInt64,
        Bool,
        Array,
        DefinedType,
        GenericDefinedType,
        Void,
        Unknown,
        EnumError,
        Reference,
        Float32,
        Float64,
        Float128,
        Err
    }
}
