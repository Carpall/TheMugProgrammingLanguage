using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zap.TypeSystem
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
        Bool,
        Array,
        DefinedType,
        GenericDefinedType,
        Void,
        Unknown,
        EnumError,
        Float32,
        Float64,
        Float128,
        Err,
        Int8,
        Int16,
        UInt16,
        Option,
        Tuple
    }
}
