using LLVMSharp.Interop;
using System;

namespace Mug.MugValueSystem
{
  public struct MugValue
    {
        public MugValueType Type { get; set; }
        public LLVMValueRef LLVMValue { get; set; }
        public bool IsConst { get; set; }
        public bool IsConstant { get; set; }

        public static MugValue From(LLVMValueRef value, MugValueType type, bool isconst = false, bool isconstant = false)
        {
            return new MugValue() { LLVMValue = value, Type = type, IsConst = isconst, IsConstant = isconstant };
        }

        public static MugValue Struct(LLVMValueRef structure, MugValueType type)
        {
            return From(structure, type);
        }

        public static MugValue Enum(MugValueType enumerated)
        {
            return From(new LLVMValueRef(), enumerated);
        }

        public static MugValue EnumMember(MugValueType enumerated, LLVMValueRef value)
        {
            return From(value, enumerated);
        }

        public bool IsAllocaInstruction()
        {
            return LLVMValue.IsAAllocaInst.Handle != IntPtr.Zero;
        }

        public bool IsGEP()
        {
            return LLVMValue.IsAGetElementPtrInst.Handle != IntPtr.Zero;
        }

        public bool IsFunction()
        {
            return LLVMValue.IsAFunction.Handle != IntPtr.Zero;
        }
    }
}
