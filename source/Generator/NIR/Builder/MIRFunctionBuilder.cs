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
        private readonly List<MIRInstruction> _body = new();
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

        public void EmitInstruction(MIRInstruction instruction)
        {
            _body.Add(instruction);
        }

        public void EmitInstruction(MIRInstructionKind kind, MIRInstruction value)
        {
            EmitInstruction(new MIRInstruction(kind, value.Type, value));
        }

        public void EmitInstruction(MIRInstructionKind kind, object value)
        {
            EmitInstruction(new MIRInstruction(kind, value: value));
        }

        public void EmitInstruction(MIRInstructionKind kind)
        {
            EmitInstruction(new MIRInstruction(kind));
        }

        public void EmitInstruction(MIRInstructionKind kind, DataType type)
        {
            EmitInstruction(new MIRInstruction(kind, type));
        }

        private void EmitInstruction(MIRInstructionKind kind, DataType type, object value)
        {
            EmitInstruction(new MIRInstruction(kind, type, value));
        }

        public void EmitLoadConstantValue(object constant, DataType type)
        {
            EmitInstruction(MIRInstructionKind.Load, type, constant);
        }

        public void EmitStoreLocal(int stackIndex, DataType type)
        {
            EmitInstruction(MIRInstructionKind.StoreLocal, type, stackIndex);
        }

        public void EmitLoadZeroinitializedStruct(DataType type)
        {
            EmitInstruction(MIRInstructionKind.LoadZeroinitialized, type);
        }

        public void EmitDupplicate()
        {
            EmitInstruction(MIRInstructionKind.Dupplicate);
        }

        public void EmitStoreField(int stackindex, DataType type)
        {
            EmitInstruction(MIRInstructionKind.StoreField, type, stackindex);
        }

        public void EmitLoadLocal(int stackindex, DataType type)
        {
            EmitInstruction(MIRInstructionKind.LoadLocal, type, stackindex);
        }

        public MIRInstruction LastInstruction()
        {
            return _body.Last();
        }

        public void EmitLoadField(int index, DataType type)
        {
            EmitInstruction(MIRInstructionKind.LoadField, type, index);
        }

        public void EmitPop()
        {
            EmitInstruction(MIRInstructionKind.Pop);
        }

        public MIRInstruction PopLastInstruction()
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
            EmitInstruction(MIRInstructionKind.JumpFalse, label);
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
            EmitInstruction(MIRInstructionKind.Jump, endLabel);
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
            EmitInstruction(MIRInstructionKind.Return, type);
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
            return _body.Count > 0 && _body[^1].Kind == MIRInstructionKind.Return;
        }

        public void EmitCall(string name, DataType type)
        {
            EmitInstruction(MIRInstructionKind.Call, type, name);
        }

        public void EmitLoadValueFromPointer()
        {
            EmitInstruction(MIRInstructionKind.LoadValueFromPointer);
        }

        public void EmitNeg(DataType type)
        {
            EmitInstruction(MIRInstructionKind.Neg, type);
        }
    }
}
