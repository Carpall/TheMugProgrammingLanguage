using Mug.Generator.IR;
using Mug.Lexer;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mug.Generator.IR.Builder
{
    internal class MIRFunctionBuilder
    {
        private readonly string _name;
        private readonly MIRType _returnType;
        private readonly MIRType[] _parameterTypes;
        private readonly List<MIRBlock> _body = new();
        private readonly List<MIRAllocation> _allocations = new();

        private List<MIRInstruction> _currentBlock = null;

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

        public void AddBlock(MIRBlock block)
        {
            _body.Add(block);
        }

        public void SwitchBlock(MIRBlock block)
        {
            _currentBlock = block.Instructions;
        }

        public void EmitInstruction(MIRInstruction instruction)
        {
            _currentBlock.Add(instruction);
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
            return _currentBlock.Last();
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
            var value = _currentBlock[^1];
            _currentBlock.RemoveAt(_currentBlock.Count - 1);
            return value;
        }

        public void EmitAutoReturn()
        {
            EmitReturn(_returnType);
        }

        public void EmitJumpCondition(int thenBlockIndex, int otherwiseBlockIndex)
        {
            EmitInstruction(MIRInstructionKind.JumpConditional, (thenBlockIndex, otherwiseBlockIndex));
        }

        public int CurrentIndex()
        {
            return _currentBlock.Count;
        }

        public int CurrentBlockIndex()
        {
            return _body.Count;
        }

        public void MoveLastInstructionTo(int index)
        {
            _currentBlock.Insert(index, PopLastInstruction());
        }

        public void EmitJump(int blockIndex)
        {
            EmitInstruction(MIRInstructionKind.Jump, blockIndex);
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
            return _currentBlock.Count > 0 && _currentBlock[^1].Kind is MIRInstructionKind.Return;
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

        public void StoreGlobal(string name, MIRType type)
        {
            EmitInstruction(new MIRInstruction(MIRInstructionKind.StoreGlobal, type, name));
        }

        public void EmitCastIntToInt(MIRType type)
        {
            EmitInstruction(MIRInstructionKind.CastIntToInt, type);
        }
    }
}
