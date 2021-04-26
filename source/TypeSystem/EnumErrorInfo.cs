using LLVMSharp.Interop;
using Mug.MugValueSystem;

namespace Mug.TypeSystem
{
  public class EnumErrorInfo
    {
        public string Name { get; set; }
        public MugValueType ErrorType { get; set; }
        public MugValueType SuccessType { get; set; }
        public LLVMTypeRef LLVMValue { get; set; }
    }
}
