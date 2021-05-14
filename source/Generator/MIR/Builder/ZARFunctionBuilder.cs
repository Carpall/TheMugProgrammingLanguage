using Zap.Models.Generator.IR;
using Zap.Models.Lexer;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zap.Models.Generator.IR.Builder
{
    internal class ZARFunctionBuilder
    {
        private readonly string _name;
        private readonly ZapType _returnType;
        private readonly ZapType[] _parameterTypes;
        private readonly List<ZARValue> _body = new();
        private readonly List<ZapType> _allocations = new();

        public ZARFunctionBuilder(string name, ZapType returntype, ZapType[] parametertypes)
        {
            _name = name;
            _returnType = returntype;
            _parameterTypes = parametertypes;
        }

        public ZARFunction Build()
        {
            return new(_name, _returnType, _parameterTypes, _body.ToArray(), _allocations.ToArray());
        }

        public void DeclareAllocation(ZapType type)
        {
            _allocations.Add(type);
        }

        public void EmitInstruction(ZARValue instruction)
        {
            _body.Add(instruction);
        }

        public void EmitInstruction(ZARValueKind kind, ZARValue value)
        {
            EmitInstruction(new ZARValue(kind, value.Type, value));
        }

        public void EmitInstruction(ZARValueKind kind)
        {
            EmitInstruction(new ZARValue(kind));
        }

        public void EmitInstruction(ZARValueKind kind, ZapType type)
        {
            EmitInstruction(new ZARValue(kind, type));
        }

        public void EmitLoadConstantValue(ZARValue constantvalue)
        {
            EmitInstruction(ZARValueKind.Load, constantvalue);
        }

        public void EmitStoreLocal(ZARValue localaddress)
        {
            EmitInstruction(ZARValueKind.StoreLocal, localaddress);
        }

        public void EmitLoadZeroinitializedStruct(ZapType type)
        {
            EmitInstruction(ZARValueKind.LoadZeroinitialized, type);
        }

        public void EmitDupplicate()
        {
            EmitInstruction(ZARValueKind.Dupplicate);
        }

        public void EmitStoreField(ZARValue fieldaddress)
        {
            EmitInstruction(ZARValueKind.StoreField, fieldaddress);
        }

        public void EmitLoadLocal(ZARValue staticMemoryAddress)
        {
            EmitInstruction(ZARValueKind.LoadLocal, staticMemoryAddress);
        }

        public ZARValue LastInstruction()
        {
            return _body.Last();
        }

        public void EmitLoadField(ZARValue fieldaddress)
        {
            EmitInstruction(ZARValueKind.LoadField, fieldaddress);
        }

        public ZARValue PopLastInstruction()
        {
            var value = _body[^1];
            _body.RemoveAt(_body.Count - 1);
            return value;
        }

        public void EmitComment(string text, bool first = true)
        {
            if (first) EmitComment(null, false);
            EmitInstruction(new ZARValue(ZARValueKind.Comment, value: text));
            if (first) EmitComment(null, false);
        }

        public int CurrentIndex()
        {
            return _body.Count;
        }

        public void MoveLastInstructionTo(int index)
        {
            _body.Insert(index, PopLastInstruction());
        }

        public int GetAllocationNumber()
        {
            return _allocations.Count - 1;
        }

        public void EmitReturn()
        {
            EmitInstruction(ZARValueKind.Return);
        }

        public void EmitOptionalReturnVoid()
        {
            if (_returnType.SolvedType.IsVoid() && (_body.Count == 0 || _body[^1].Kind != ZARValueKind.Return))
            {
                EmitComment("implicit void return");
                EmitReturn();
            }
        }

        public void EmitCall(string name, ZapType type)
        {
            EmitInstruction(ZARValueKind.Call, ZARValue.MemberIdentifier(name, type));
        }
    }
}
