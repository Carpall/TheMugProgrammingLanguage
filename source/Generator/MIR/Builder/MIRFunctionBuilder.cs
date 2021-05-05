using Zap.Models.Generator.IR;
using Zap.Models.Lexer;
using Zap.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zap.Models.Generator.IR.Builder
{
    internal class MIRFunctionBuilder
    {
        private readonly string _name;
        private readonly ZapType _returnType;
        private readonly ZapType[] _parameterTypes;
        private readonly List<MIRValue> _body = new();
        private readonly List<ZapType> _allocations = new();

        public MIRFunctionBuilder(string name, ZapType returntype, ZapType[] parametertypes)
        {
            _name = name;
            _returnType = returntype;
            _parameterTypes = parametertypes;
        }

        public MIRFunction Build()
        {
            return new(_name, _returnType, _parameterTypes, _body.ToArray(), _allocations.ToArray());
        }

        public void DeclareAllocation(ZapType type)
        {
            _allocations.Add(type);
        }

        public void EmitInstruction(MIRValue instruction)
        {
            _body.Add(instruction);
        }

        public void EmitInstruction(MIRValueKind kind, MIRValue value)
        {
            EmitInstruction(new MIRValue(kind, value.Type, value));
        }

        public void EmitInstruction(MIRValueKind kind)
        {
            EmitInstruction(new MIRValue(kind));
        }

        public void EmitInstruction(MIRValueKind kind, ZapType type)
        {
            EmitInstruction(new MIRValue(kind, type));
        }

        public void EmitLoadConstantValue(MIRValue constantvalue)
        {
            EmitInstruction(MIRValueKind.Load, constantvalue);
        }

        public void EmitStoreLocal(MIRValue localaddress)
        {
            EmitInstruction(MIRValueKind.StoreLocal, localaddress);
        }

        public void EmitLoadZeroinitializedStruct(ZapType type)
        {
            EmitInstruction(MIRValueKind.LoadZeroinitialized, type);
        }

        public void EmitDupplicate()
        {
            EmitInstruction(MIRValueKind.Dupplicate);
        }

        public void EmitStoreField(MIRValue fieldaddress)
        {
            EmitInstruction(MIRValueKind.StoreField, fieldaddress);
        }

        public void EmitLoadLocal(MIRValue staticMemoryAddress)
        {
            EmitInstruction(MIRValueKind.LoadLocal, staticMemoryAddress);
        }

        public MIRValue LastInstruction()
        {
            return _body.Last();
        }

        public void EmitLoadField(MIRValue fieldaddress)
        {
            EmitInstruction(MIRValueKind.LoadField, fieldaddress);
        }

        public MIRValue PopLastInstruction()
        {
            var value = _body[^1];
            _body.RemoveAt(_body.Count - 1);
            return value;
        }

        public void EmitComment(string text, bool first = true)
        {
            if (first) EmitComment(null, false);
            EmitInstruction(new MIRValue(MIRValueKind.Comment, value: text));
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
            EmitInstruction(MIRValueKind.Return);
        }

        public void EmitOptionalReturnVoid()
        {
            if (_returnType.SolvedType.IsVoid() && (_body.Count == 0 || _body[^1].Kind != MIRValueKind.Return))
            {
                EmitComment("implicit void return");
                EmitReturn();
            }
        }
    }
}
