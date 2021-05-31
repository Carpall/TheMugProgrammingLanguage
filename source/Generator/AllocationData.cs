using Mug.TypeSystem;

namespace Mug.Generator
{
    public record AllocationData(int StackIndex, DataType Type, bool IsConst);
}
