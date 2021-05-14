using Nylon.Models.Generator.IR;
using Nylon.Models.Lexer;
using Nylon.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nylon.Models.Generator.IR.Builder
{
    internal class NIRFunctionBuilder
    {
        private readonly string _name;
        private readonly DataType _returnType;
        private readonly DataType[] _parameterTypes;
        private readonly List<NIRValue> _body = new();
        private readonly List<DataType> _allocations = new();

        public NIRFunctionBuilder(string name, DataType returntype, DataType[] parametertypes)
        {
            _name = name;
            _returnType = returntype;
            _parameterTypes = parametertypes;
        }

        public NIRFunction Build()
        {
            return new(_name, _returnType, _parameterTypes, _body.ToArray(), _allocations.ToArray());
        }

        public void DeclareAllocation(DataType type)
        {
            _allocations.Add(type);
        }

        public void EmitInstruction(NIRValue instruction)
        {
            _body.Add(instruction);
        }

        public void EmitInstruction(NIRValueKind kind, NIRValue value)
        {
            EmitInstruction(new NIRValue(kind, value.Type, value));
        }

        public void EmitInstruction(NIRValueKind kind)
        {
            EmitInstruction(new NIRValue(kind));
        }

        public void EmitInstruction(NIRValueKind kind, DataType type)
        {
            EmitInstruction(new NIRValue(kind, type));
        }

        public void EmitLoadConstantValue(NIRValue constantvalue)
        {
            EmitInstruction(NIRValueKind.Load, constantvalue);
        }

        public void EmitStoreLocal(NIRValue localaddress)
        {
            EmitInstruction(NIRValueKind.StoreLocal, localaddress);
        }

        public void EmitLoadZeroinitializedStruct(DataType type)
        {
            EmitInstruction(NIRValueKind.LoadZeroinitialized, type);
        }

        public void EmitDupplicate()
        {
            EmitInstruction(NIRValueKind.Dupplicate);
        }

        public void EmitStoreField(NIRValue fieldaddress)
        {
            EmitInstruction(NIRValueKind.StoreField, fieldaddress);
        }

        public void EmitLoadLocal(NIRValue staticMemoryAddress)
        {
            EmitInstruction(NIRValueKind.LoadLocal, staticMemoryAddress);
        }

        public NIRValue LastInstruction()
        {
            return _body.Last();
        }

        public void EmitLoadField(NIRValue fieldaddress)
        {
            EmitInstruction(NIRValueKind.LoadField, fieldaddress);
        }

        public void EmitPop()
        {
            EmitInstruction(NIRValueKind.Pop);
        }

        public NIRValue PopLastInstruction()
        {
            var value = _body[^1];
            _body.RemoveAt(_body.Count - 1);
            return value;
        }

        public void EmitComment(string text, bool first = true)
        {
            if (first) EmitComment(null, false);
            EmitInstruction(new NIRValue(NIRValueKind.Comment, value: text));
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
            EmitInstruction(NIRValueKind.Return);
        }

        public void EmitOptionalReturnVoid()
        {
            if (_returnType.SolvedType.IsVoid() && (_body.Count == 0 || _body[^1].Kind != NIRValueKind.Return))
            {
                EmitComment("implicit void return");
                EmitReturn();
            }
        }

        public void EmitCall(string name, DataType type)
        {
            EmitInstruction(NIRValueKind.Call, NIRValue.MemberIdentifier(name, type));
        }
    }
}
