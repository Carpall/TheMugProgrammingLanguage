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
        private readonly MIRType _returnType;
        private readonly MIRType[] _parameterTypes;
        private readonly List<MIRInstruction> _body = new();
        private readonly List<MIRAllocation> _allocations = new();

        public MIRFunctionBuilder(string name, MIRType returntype, MIRType[] parametertypes)
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
            return new(_name, _returnType, _parameterTypes, _body.ToArray(), _allocations.ToArray());
        }

        public void DeclareAllocation(MIRAllocationAttribute attributes, MIRType type)
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

        public void EmitInstruction(MIRInstructionKind kind, MIRType type)
        {
            EmitInstruction(new MIRInstruction(kind, type));
        }

        private void EmitInstruction(MIRInstructionKind kind, MIRType type, object value)
        {
            EmitInstruction(new MIRInstruction(kind, type, value));
        }

        public void EmitLoadConstantValue(object constant, MIRType type)
        {
            EmitInstruction(MIRInstructionKind.Load, type, constant);
        }

        public void EmitStoreLocal(int stackIndex, MIRType type)
        {
            EmitInstruction(MIRInstructionKind.StoreLocal, type, stackIndex);
        }

        public void EmitLoadZeroinitializedStruct(MIRType type)
        {
            EmitInstruction(MIRInstructionKind.LoadZeroinitialized, type);
        }

        public void EmitDupplicate()
        {
            EmitInstruction(MIRInstructionKind.Dupplicate);
        }

        public void EmitStoreField(int stackindex, MIRType type)
        {
            EmitInstruction(MIRInstructionKind.StoreField, type, stackindex);
        }

        public void EmitLoadLocal(int stackindex, MIRType type)
        {
            EmitInstruction(MIRInstructionKind.LoadLocal, type, stackindex);
        }

        public MIRInstruction LastInstruction()
        {
            return _body.Last();
        }

        public void EmitLoadField(int index, MIRType type)
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

        public int CreateLabelHere()
        {
            var currentBodyIndex = CurrentIndex();
            EmitInstruction(MIRInstructionKind.Label, currentBodyIndex);
            return currentBodyIndex;
        }

        public void EmitAutoReturn()
        {
            EmitReturn(_returnType);
        }

        public void EmitJumpFalse(MIRLabel label)
        {
            EmitInstruction(MIRInstructionKind.JumpFalse, label);
        }

        public int CurrentIndex()
        {
            return _body.Count;
        }

        public void MoveLastInstructionTo(int index)
        {
            _body.Insert(index, PopLastInstruction());
        }

        public void EmitJump(MIRLabel labeò)
        {
            EmitInstruction(MIRInstructionKind.Jump, labeò);
        }

        public int GetAllocationsNumber()
        {
            return _allocations.Count;
        }

        public void EmitReturn(MIRType type)
        {
            EmitInstruction(MIRInstructionKind.Return, type);
        }

        public void EmitOptionalReturnVoid()
        {
            if (_returnType.IsVoid() && !EmittedExplicitReturn())
                EmitReturn(new(MIRTypeKind.Void));
        }

        private bool EmittedExplicitReturn()
        {
            return _body.Count > 0 && _body[^1].Kind == MIRInstructionKind.Return;
        }

        public void EmitCall(string name, MIRType type)
        {
            EmitInstruction(MIRInstructionKind.Call, type, name);
        }

        public void EmitLoadValueFromPointer()
        {
            EmitInstruction(MIRInstructionKind.LoadValueFromPointer);
        }

        public void EmitNeg(MIRType type)
        {
            EmitInstruction(MIRInstructionKind.Neg, type);
        }
    }
}
