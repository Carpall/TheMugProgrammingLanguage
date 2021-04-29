using Mug.Models.Generator.IR;
using Mug.Models.Lexer;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;

namespace Mug.Models.Generator.IR.Builder
{
    internal class MIRFunctionBuilder
    {
        private readonly string _name;
        private readonly MIRType _returnType;
        private readonly MIRType[] _parameterTypes;
        private readonly List<MIRValue> _body = new();
        private readonly List<MIRType> _allocations = new();

        public MIRFunctionBuilder(string name, MIRType returntype, MIRType[] parametertypes)
        {
            _name = name;
            _returnType = returntype;
            _parameterTypes = parametertypes;
        }

        public MIRFunction Build()
        {
            return new(_name, _returnType, _parameterTypes, _body.ToArray(), _allocations.ToArray());
        }

        public void DeclareAllocation(MIRType type)
        {
            _allocations.Add(type);
        }

        public void EmitInstruction(MIRValueKind kind, MIRValue value)
        {
            _body.Add(new(kind, value.Type, value));
        }

        public void EmitInstruction(MIRValueKind kind)
        {
            _body.Add(new(kind));
        }

        public void EmitInstruction(MIRValueKind kind, MIRType type)
        {
            _body.Add(new(kind, type));
        }

        public void EmitLoadConstantValue(MIRValue constantvalue)
        {
            EmitInstruction(MIRValueKind.Load, constantvalue);
        }

        public void EmitStoreLocal(MIRValue localaddress)
        {
            EmitInstruction(MIRValueKind.Store, localaddress);
        }

        public void EmitLoadZeroinitializedStruct(MIRType type)
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
    }
}
