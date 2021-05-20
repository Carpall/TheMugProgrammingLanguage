using Mug.Models.Generator.IR;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mug.Models.Generator.IR.Builder
{
    internal class MIRFunctionBuilder
    {
        private readonly string _name;
        private readonly DataType _returnType;
        private readonly DataType[] _parameterTypes;
        private readonly List<MIRValue> _body = new();
        private readonly List<MIRAllocation> _allocations = new();
        private readonly List<MIRLabel> _labels = new();

        public MIRFunctionBuilder(string name, DataType returntype, DataType[] parametertypes)
        {
            _name = name;
            _returnType = returntype;
            _parameterTypes = parametertypes;
        }

        public MIRFunctionBuilder(MIRFunctionBuilder functionBuilder)
        {
            _name = functionBuilder._name;
            _returnType = functionBuilder._returnType;
            _parameterTypes = functionBuilder._parameterTypes;
            _body = new(functionBuilder._body);
            _allocations = new(functionBuilder._allocations);
        }

        public MIRFunction Build()
        {
            return new(_name, _returnType, _parameterTypes, _body.ToArray(), _allocations.ToArray(), _labels.ToArray());
        }

        public void DeclareAllocation(MIRAllocationAttribute attributes, DataType type)
        {
            _allocations.Add(new(attributes, type));
        }

        public void EmitInstruction(MIRValue instruction)
        {
            _body.Add(instruction);
        }

        public void EmitInstruction(MIRValueKind kind, MIRValue value)
        {
            EmitInstruction(new MIRValue(kind, value.Type, value));
        }

        public void EmitInstruction(MIRValueKind kind, object value)
        {
            EmitInstruction(new MIRValue(kind, value: value));
        }

        public void EmitInstruction(MIRValueKind kind)
        {
            EmitInstruction(new MIRValue(kind));
        }

        public void EmitInstruction(MIRValueKind kind, DataType type)
        {
            EmitInstruction(new MIRValue(kind, type));
        }

        private void EmitInstruction(MIRValueKind kind, DataType type, object value)
        {
            EmitInstruction(new MIRValue(kind, type, value));
        }

        public void EmitLoadConstantValue(object constant, DataType type)
        {
            EmitInstruction(MIRValueKind.Load, type, constant);
        }

        public void EmitStoreLocal(int stackIndex, DataType type)
        {
            EmitInstruction(MIRValueKind.StoreLocal, type, stackIndex);
        }

        public void EmitLoadZeroinitializedStruct(DataType type)
        {
            EmitInstruction(MIRValueKind.LoadZeroinitialized, type);
        }

        public void EmitDupplicate()
        {
            EmitInstruction(MIRValueKind.Dupplicate);
        }

        public void EmitStoreField(int stackindex, DataType type)
        {
            EmitInstruction(MIRValueKind.StoreField, type, stackindex);
        }

        public void EmitLoadLocal(int stackindex, DataType type)
        {
            EmitInstruction(MIRValueKind.LoadLocal, type, stackindex);
        }

        public MIRValue LastInstruction()
        {
            return _body.Last();
        }

        public void EmitLoadField(int index, DataType type)
        {
            EmitInstruction(MIRValueKind.LoadField, type, index);
        }

        public void EmitPop()
        {
            EmitInstruction(MIRValueKind.Pop);
        }

        public MIRValue PopLastInstruction()
        {
            var value = _body[^1];
            _body.RemoveAt(_body.Count - 1);
            return value;
        }

        public void EmitAutoReturn()
        {
            EmitReturn(_returnType);
        }

        public void EmitJumpFalse(MIRLabel label)
        {
            EmitInstruction(MIRValueKind.JumpFalse, label);
        }

        public MIRLabel CreateLabel(string label)
        {
            var irlabel = new MIRLabel(CurrentIndex(), label);
            _labels.Add(irlabel);
            return irlabel;
        }

        public void EmitComment(string text, bool first = true)
        {
            /*if (first) EmitComment(null, false);
            EmitInstruction(MIRValueKind.Comment, text);
            if (first) EmitComment(null, false);*/
        }

        public int CurrentIndex()
        {
            return _body.Count;
        }

        public void MoveLastInstructionTo(int index)
        {
            _body.Insert(index, PopLastInstruction());
        }

        public void EmitJump(MIRLabel endLabel)
        {
            EmitInstruction(MIRValueKind.Jump, endLabel);
        }

        public int GetAllocationNumber()
        {
            return _allocations.Count - 1;
        }

        public int GetAllocationNumbers()
        {
            return _allocations.Count;
        }

        public void EmitReturn(DataType type)
        {
            EmitInstruction(MIRValueKind.Return, type);
        }

        public void EmitOptionalReturnVoid()
        {
            if (_returnType.SolvedType.IsVoid() && !EmittedExplicitReturn())
            {
                EmitComment("implicit void return");
                EmitReturn(DataType.Void);
            }
        }

        private bool EmittedExplicitReturn()
        {
            return _body.Count > 0 && _body[^1].Kind == MIRValueKind.Return;
        }

        public void EmitCall(string name, DataType type)
        {
            EmitInstruction(MIRValueKind.Call, type, name);
        }
    }
}
