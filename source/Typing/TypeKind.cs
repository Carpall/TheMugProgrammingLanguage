using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.TypeSystem
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypeKind
    {
        Undefined,
        Auto,
        Pointer,
        String,
        Char,
        Int32,
        Int64,
        UInt8,
        UInt32,
        UInt64,
        Float32,
        Float64,
        Float128,
        Int8,
        Int16,
        UInt16,
        Bool,
        Array,
        DefinedType,
        GenericDefinedType,
        Void,
        Unknown,
        EnumError,
        Err,
        Option,
        Tuple
    }
}
