using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mug.TypeSystem
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypeKind
    {
        String,
        Int8 = 4,
        Int16 = 8,
        Int32 = 16,
        Int64 = 32,
        UInt8 = 5,
        UInt16 = 9,
        UInt32 = 17,
        UInt64 = 33,
        Float32,
        Float64,
        Float128,
        Char,
        Bool,
        Err,
        Undefined,
        Auto,
        Pointer,
        Array,
        DefinedType,
        GenericDefinedType,
        Void,
        EnumError,
        Option,
        Tuple
    }
}
