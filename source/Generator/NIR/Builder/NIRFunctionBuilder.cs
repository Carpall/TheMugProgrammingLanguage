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
        private readonly List<NIRAllocation> _allocations = new();
        private int _labelEnumeration = 0;

        public NIRFunctionBuilder(string name, DataType returntype, DataType[] parametertypes)
        {
            _name = name;
            _returnType = returntype;
            _parameterTypes = parametertypes;
        }

        public NIRFunctionBuilder(NIRFunctionBuilder functionBuilder)
        {
            _name = functionBuilder._name;
            _returnType = functionBuilder._returnType;
            _parameterTypes = functionBuilder._parameterTypes;
            _body = new(functionBuilder._body);
            _allocations = new(functionBuilder._allocations);
        }

        public NIRFunction Build()
        {
            return new(_name, _returnType, _parameterTypes, _body.ToArray(), _allocations.ToArray());
        }

        public void DeclareAllocation(NIRAllocationAttribute attributes, DataType type)
        {
            _allocations.Add(new(attributes, type));
        }

        public void EmitInstruction(NIRValue instruction)
        {
            _body.Add(instruction);
        }

        public void EmitInstruction(NIRValueKind kind, NIRValue value)
        {
            EmitInstruction(new NIRValue(kind, value.Type, value));
        }

        public void EmitInstruction(NIRValueKind kind, object value)
        {
            EmitInstruction(new NIRValue(kind, value: value));
        }

        public void EmitInstruction(NIRValueKind kind)
        {
            EmitInstruction(new NIRValue(kind));
        }

        public void EmitInstruction(NIRValueKind kind, DataType type)
        {
            EmitInstruction(new NIRValue(kind, type));
        }

        private void EmitInstruction(NIRValueKind kind, DataType type, object value)
        {
            EmitInstruction(new NIRValue(kind, type, value));
        }

        public void EmitLoadConstantValue(object constant, DataType type)
        {
            EmitInstruction(NIRValueKind.Load, type, constant);
        }

        public void EmitStoreLocal(int stackIndex, DataType type)
        {
            EmitInstruction(NIRValueKind.StoreLocal, type, stackIndex);
        }

        public void EmitLoadZeroinitializedStruct(DataType type)
        {
            EmitInstruction(NIRValueKind.LoadZeroinitialized, type);
        }

        public void EmitDupplicate()
        {
            EmitInstruction(NIRValueKind.Dupplicate);
        }

        public void EmitStoreField(int stackindex, DataType type)
        {
            EmitInstruction(NIRValueKind.StoreField, type, stackindex);
        }

        public void EmitLoadLocal(int stackindex, DataType type)
        {
            EmitInstruction(NIRValueKind.LoadLocal, type, stackindex);
        }

        public NIRValue LastInstruction()
        {
            return _body.Last();
        }

        public void EmitLoadField(int index, DataType type)
        {
            EmitInstruction(NIRValueKind.LoadField, type, index);
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

        public void EmitAutoReturn()
        {
            EmitReturn(_returnType);
        }

        public string EmitJumpFalse(string label)
        {
            label = EnumerateLabel(label);
            EmitInstruction(NIRValueKind.JumpFalse, DataType.Void, label);
            return label;
        }

        private string EnumerateLabel(string label)
        {
            return $"{label}{_labelEnumeration++}";
        }

        public void EmitComment(string text, bool first = true)
        {
            if (first) EmitComment(null, false);
            EmitInstruction(NIRValueKind.Comment, text);
            if (first) EmitComment(null, false);
        }

        public void EmitLabel(string label)
        {
            EmitInstruction(NIRValueKind.Label, label);
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

        public int GetAllocationNumbers()
        {
            return _allocations.Count;
        }

        public void EmitReturn(DataType type)
        {
            EmitInstruction(NIRValueKind.Return, type);
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
            return _body.Count > 0 && _body[^1].Kind == NIRValueKind.Return;
        }

        public void EmitCall(string name, DataType type)
        {
            EmitInstruction(NIRValueKind.Call, type, name);
        }
    }
}
