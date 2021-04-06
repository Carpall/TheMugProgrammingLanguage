using LLVMSharp.Interop;
using Mug.Compilation;
using Mug.Compilation.Symbols;
using Mug.Models.Lexer;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds.Statements;
using Mug.MugValueSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Mug.Models.Generator.Emitter
{
    internal class MugEmitter
    {
        public unsafe LLVMBuilderRef Builder { get; internal set; } = LLVM.CreateBuilder();

        private readonly Stack<MugValue> _stack = new();
        private readonly IRGenerator _generator;
        public Dictionary<string, MugValue> Memory { get; set; }
        internal LLVMBasicBlockRef ExitBlock { get; }
        internal bool IsInsideSubBlock { get; }

        public MugEmitter(IRGenerator generator, Dictionary<string, MugValue> memory, LLVMBasicBlockRef exitblock, bool isInsideSubBlock)
        {
            _generator = generator;
            // copying the symbols instead of passing them for reference
            Memory = new(memory);
            ExitBlock = exitblock;
            IsInsideSubBlock = isInsideSubBlock;
        }

        public MugEmitter(IRGenerator generator, LLVMBasicBlockRef exitblock, bool isInsideSubBlock)
        {
            _generator = generator;
            Memory = new();
            ExitBlock = exitblock;
            IsInsideSubBlock = isInsideSubBlock;
        }

        public void Report(Range position, string error)
        {
            _generator.Report(position, error);
        }

        public void Load(MugValue value)
        {
            _stack.Push(value);
        }

        public MugValue Pop()
        {
            return _stack.Pop();
        }

        public MugValue Peek()
        {
            return _stack.Peek();
        }

        public MugValueType PeekType()
        {
            return Peek().Type;
        }

        public void AddInt(MugValue left, MugValue right)
        {
            Load(
                MugValue.From(Builder.BuildAdd(left.LLVMValue, right.LLVMValue), right.Type, isconstant: left.IsConstant && right.IsConstant));
        }

        public void SubInt(MugValue left, MugValue right)
        {
            Load(
                MugValue.From(Builder.BuildSub(left.LLVMValue, right.LLVMValue), right.Type, isconstant: left.IsConstant && right.IsConstant));
        }

        public void MulInt(MugValue left, MugValue right)
        {
            Load(
                MugValue.From(Builder.BuildMul(left.LLVMValue, right.LLVMValue), right.Type, isconstant: left.IsConstant && right.IsConstant));
        }

        public void DivInt(MugValue left, MugValue right)
        {
            Load(
                MugValue.From(Builder.BuildSDiv(left.LLVMValue, right.LLVMValue), right.Type, isconstant: left.IsConstant && right.IsConstant));
        }

        public void CastInt(MugValueType type)
        {
            var value = Pop();
            Load(
                MugValue.From(Builder.BuildIntCast(value.LLVMValue, type.LLVMType), type, isconstant: value.IsConstant));
        }

        public void CastIntToFloat(MugValueType type)
        {
            var value = Pop();
            Load(
                MugValue.From(Builder.BuildSIToFP(value.LLVMValue, type.LLVMType), type, isconstant: value.IsConstant));
        }

        public MugValue? GetMemoryAllocation(string name, Range position)
        {
            if (!Memory.TryGetValue(name, out var variable))
            {
                Report(position, "Undeclared item");
                return null;
            }

            return variable;
        }

        private void SetMemory(string name, MugValue value)
        {
            Memory.Add(name, value);
        }

        public void DeclareVariable(VariableStatement variable)
        {
            DeclareVariable(
                variable.Name,
                variable.Type.ToMugValueType(_generator),
                variable.Position);
        }

        public void GetCoupleValues(out MugValue first, out MugValue second)
        {
            second = Pop();
            first = Pop();
        }

        private void CoerceCoupleConstantIntSize()
        {
            var second = Pop();
            var first = Pop();

            if (first.IsConstant && second.IsConstant)
            {
                if (second.Type.Size(_generator.SizeOfPointer) >= first.Type.Size(_generator.SizeOfPointer))
                    first = MugValue.From(Builder.BuildIntCast(first.LLVMValue, second.Type.LLVMType), second.Type, first.IsConst, first.IsConstant);
                else
                    second = MugValue.From(Builder.BuildIntCast(second.LLVMValue, first.Type.LLVMType), first.Type, second.IsConst, second.IsConstant);
            }
            else if (second.Type.MatchIntType() && first.Type.MatchIntType())
            {
                if (second.IsConstant)
                    second = MugValue.From(Builder.BuildIntCast(second.LLVMValue, first.Type.LLVMType), first.Type, first.IsConst, second.IsConstant);
                else if (first.IsConstant)
                    first = MugValue.From(Builder.BuildIntCast(first.LLVMValue, second.Type.LLVMType), second.Type, second.IsConst, first.IsConstant);
            }

            Load(first);
            Load(second);
        }

        private void CoerceCoupleConstantFloatSize()
        {
            var second = Pop();
            var first = Pop();

            Console.WriteLine();
            if (first.IsConstant && second.IsConstant)
            {
                if (second.Type.Size(_generator.SizeOfPointer) >= first.Type.Size(_generator.SizeOfPointer))
                    first = MugValue.From(Builder.BuildFPCast(first.LLVMValue, second.Type.LLVMType), second.Type, first.IsConst, first.IsConstant);
                else
                    second = MugValue.From(Builder.BuildFPCast(second.LLVMValue, first.Type.LLVMType), first.Type, second.IsConst, second.IsConstant);
            }
            else if (second.Type.MatchFloatType() && first.Type.MatchFloatType())
            {
                if (second.IsConstant)
                    second = MugValue.From(Builder.BuildFPCast(second.LLVMValue, first.Type.LLVMType), first.Type, first.IsConst, first.IsConstant);
                else if (first.IsConstant)
                    first = MugValue.From(Builder.BuildFPCast(first.LLVMValue, second.Type.LLVMType), second.Type, second.IsConst, second.IsConstant);
            }

            Load(first);
            Load(second);
        }

        private void CoerceCoupleConstantFloatIntSize()
        {
            var second = Pop();
            var first = Pop();

            if (first.IsConstant && second.IsConstant)
            {
                if (first.Type.MatchIntType() && second.Type.MatchFloatType())
                    first = MugValue.From(Builder.BuildFPCast(first.LLVMValue, second.Type.LLVMType), second.Type, first.IsConst, first.IsConstant);
                else if (second.Type.MatchIntType() && first.Type.MatchFloatType())
                    second = MugValue.From(Builder.BuildFPCast(second.LLVMValue, first.Type.LLVMType), first.Type, second.IsConst, second.IsConstant);
            }

            Load(first);
            Load(second);
        }

        public void CoerceCoupleConstantSize()
        {
            if (PeekType().MatchIntType())
            {
                CoerceCoupleConstantIntSize();
                CoerceCoupleConstantFloatIntSize();
            }
            else if (PeekType().MatchFloatType())
                CoerceCoupleConstantFloatSize();
        }

        private void CoerceConstantIntSizeTo(MugValueType type)
        {
            var value = Pop();

            if (value.Type.MatchIntType() && value.IsConstant)
                value = MugValue.From(Builder.BuildIntCast(value.LLVMValue, type.LLVMType), type, value.IsConst, value.IsConstant);

            Load(value);
        }

        private void CoerceConstantFloatSizeTo(MugValueType type)
        {
            var value = Pop();

            if (value.IsConstant)
            {
                if (value.Type.MatchIntType())
                    value = MugValue.From(Builder.BuildIntCast(value.LLVMValue, type.LLVMType), type, value.IsConst, value.IsConstant);
                else if (value.Type.MatchFloatType())
                    value = MugValue.From(Builder.BuildFPCast(value.LLVMValue, type.LLVMType), type, value.IsConst, value.IsConstant);
            }

            Load(value);
        }

        public void CoerceConstantSizeTo(MugValueType type)
        {
            if (type.MatchIntType())
                CoerceConstantIntSizeTo(type);
            else if (type.MatchFloatType())
                CoerceConstantFloatSizeTo(type);
        }

        private void MakeConst()
        {
            var value = Pop();

            value.IsConst = true;

            Load(value);
        }

        private bool AlreadyDeclared(string name, Range position)
        {
            if (IsDeclared(name))
            {
                _generator.Report(position, "Variable already declared");
                return true;
            }

            return false;
        }

        public void DeclareVariable(string name, MugValueType type, Range position)
        {
            if (AlreadyDeclared(name, position))
                return;

            SetMemory(
                name,
                MugValue.From(Builder.BuildAlloca(type.LLVMType, name), type));
        }

        public void DeclareConstant(string name, Range position)
        {
            if (AlreadyDeclared(name, position))
                return;

            MakeConst();

            SetMemory(name, Pop());
        }

        public void StoreVariable(string name, Range position, Range bodyPosition)
        {
            var allocation = GetMemoryAllocation(name, position);
            if (!allocation.HasValue)
                return;

            StoreVariable(allocation.Value, position, bodyPosition);
        }

        public void InitializeParameter(string name, LLVMValueRef llvmparameter)
        {
            Builder.BuildStore(llvmparameter, Memory[name].LLVMValue);
        }

        public void StoreVariable(MugValue allocation, Range position, Range bodyPosition)
        {
            // check it is a variable and not a constant
            if (allocation.IsConst)
            {
                Report(position, "Unable to change the value of a constant");
                return;
            }

            CoerceConstantSizeTo(allocation.Type);

            _generator.ExpectSameTypes(
                allocation.Type,
                bodyPosition,
                $"Expected '{allocation.Type}' type, got '{PeekType()}' type",
                PeekType());

            Builder.BuildStore(Pop().LLVMValue, allocation.LLVMValue);
        }

        /*public bool OneOfTwoIsOnlyTheEnumType(MugValue first, MugValue second)
        {
            return second.LLVMValue.Handle == IntPtr.Zero || first.LLVMValue.Handle == IntPtr.Zero;
        }*/

        public bool IsDeclared(string name)
        {
            return Memory.ContainsKey(name);
        }

        public bool LoadFromMemory(string name, Range position)
        {
            var variable = GetMemoryAllocation(name, position);
            if (!variable.HasValue)
                return false;

            // variable
            if (!variable.Value.IsConst)
            {
                /*if (variable.Type.IsPointer())
                    EmitGCIncrementReferenceCounter(variable.LLVMValue);*/

                variable = MugValue.From(Builder.BuildLoad(variable.Value.LLVMValue), variable.Value.Type);

                Load(variable.Value);
            }
            else // constant
                Load(variable.Value);

            return true;
        }

        public bool CallOperator(FunctionSymbol? function, Range position, bool expectedNonVoid, params MugValue[] types)
        {
            if (function is null)
                return false;

            var functionRetType = function.Value.ReturnType;

            if (expectedNonVoid)
                // check the operator overloading is not void
                _generator.ExpectNonVoidType(functionRetType.LLVMType, position);

            Call(function.Value.Value.LLVMValue, types, functionRetType, null);

            return true;
        }

        public void CallAsOperator(Range position, MugValueType type, MugValueType returntype)
        {
            // tofix
            throw new();
            /*var function = _generator.EvaluateFunction($"as({type}): {returntype}", null, Array.Empty<MugValueType>(), Array.Empty<MugValueType>(), position, true);

            Call(function.Value.LLVMValue, 1, returntype, false);*/
        }

        public void Ret()
        {
            Builder.BuildRet(Pop().LLVMValue);
        }

        public void RetVoid()
        {
            Builder.BuildRetVoid();
        }

        public void CompareInt(LLVMIntPredicate kind, MugValue left, MugValue right)
        {
            Load(MugValue.From(Builder.BuildICmp(kind, left.LLVMValue, right.LLVMValue), MugValueType.Bool, isconstant: right.IsConstant && left.IsConstant));
        }

        public void NegInt()
        {
            var value = Pop();
            Load(MugValue.From(Builder.BuildNeg(value.LLVMValue), value.Type, isconstant: value.IsConstant));
        }

        public void NegBool()
        {
            var value = Pop();
            Load(MugValue.From(Builder.BuildNot(value.LLVMValue), MugValueType.Bool, isconstant: value.IsConstant));
        }

        /// <summary>
        /// buils an array of the parameters to pass and calls the function
        /// </summary>
        public void Call(LLVMValueRef function, MugValue[] parameters, MugValueType returnType, MugValue? basevalue)
        {
            var llvmparameters = _generator.MugValuesToLLVMValues(parameters);

            var result = Builder.BuildCall(function, basevalue.HasValue ? llvmparameters.Prepend(basevalue.Value.LLVMValue).ToArray() : llvmparameters);

            if (result.TypeOf.Kind != LLVMTypeKind.LLVMVoidTypeKind)
                Load(MugValue.From(result, returnType));
        }

        public void CastEnumMemberToBaseType(MugValueType type)
        {
            Load(MugValue.From(Pop().LLVMValue, type));
        }

        public void CastToEnumMemberFromBaseType(MugValueType enumerated)
        {
            Load(MugValue.From(Pop().LLVMValue, enumerated));
        }

        public void CompareJump(LLVMBasicBlockRef ifbody, LLVMBasicBlockRef elsebody)
        {
            Builder.BuildCondBr(Pop().LLVMValue, ifbody, elsebody);
        }

        public void JumpOutOfScope(LLVMValueRef terminator, LLVMBasicBlockRef targetblock)
        {
            // check if the block has not terminator yet
            if (terminator.Handle == IntPtr.Zero)
                Jump(targetblock);
        }

        public void Jump(LLVMBasicBlockRef targetblock)
        {
            Builder.BuildBr(targetblock);
        }

        public bool LoadMemoryAllocation(string name, Range position)
        {
            var allocation = GetMemoryAllocation(name, position);
            if (allocation.HasValue)
                return false;

            if (!allocation.Value.IsAllocaInstruction())
            {
                var tmp = Builder.BuildAlloca(allocation.Value.Type.LLVMType);
                Builder.BuildStore(allocation.Value.LLVMValue, tmp);

                allocation = MugValue.From(tmp, allocation.Value.Type);
            }

            Load(allocation.Value);
            return true;
        }

        public void StoreField(LLVMValueRef tmp, int index)
        {
            Builder.BuildStore(
                Pop().LLVMValue,
                Builder.BuildGEP(
                    tmp,
                    new[]
                    {
                        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)index)
                    })
                );
        }

        public bool LoadEnumMember(string enumname, string membername, Range baseposition, Range memberposition, LocalGenerator localgenerator)
        {
            var enumerated = _generator.Table.GetEnumType(enumname, baseposition, true);
            if (!enumerated.HasValue)
                return false;

            if (!enumerated.Value.Type.IsEnum())
            {
                if (enumerated.Value.Type.IsEnumError())
                {
                    var enumerror = enumerated.Value.Type.GetEnumError();
                    var index = enumerror.Body.FindIndex(member => member.Value == membername);

                    if (index == -1)
                    {
                        _generator.Report(baseposition, $"'{enumname}' does not contain a definition for '{membername}'");
                        return false;
                    }

                    Load(MugValue.EnumMember(enumerated.Value.Type, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, (uint)index)));
                    return true;
                }
                else
                {
                    _generator.Report(baseposition, "Not an enum");
                    return false;
                }
            }

            var type = enumerated.Value.Type.GetEnum();

            Load(type.GetMemberValueFromName(enumerated.Value.Type, type.BaseType.ToMugValueType(_generator), membername, memberposition, localgenerator));
            return true;
        }

        public void LoadField(MugValue instance, MugValueType fieldType, int index, bool load)
        {
            var field = Builder.BuildGEP(
                instance.LLVMValue,
                new[]
                {
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)index)
                });

            Load(
                MugValue.From(
                    load ? Builder.BuildLoad(field) : field,
                    fieldType)
                );
        }

        public bool LoadFieldName(string name, Range position)
        {
            var allocation = GetMemoryAllocation(name, position);
            if (!allocation.HasValue)
                return false;

            var instance = allocation.Value;

            if (!instance.IsAllocaInstruction())
            {
                var tmp = Builder.BuildAlloca(instance.Type.LLVMType);
                Builder.BuildStore(instance.LLVMValue, tmp);
                instance.LLVMValue = tmp;
            }
            
            Load(instance);
            return true;
        }

        public void LoadFieldName()
        {
            var value = Pop();
            
            if (value.LLVMValue.IsALoadInst.Handle != IntPtr.Zero)
                Load(MugValue.From(value.LLVMValue.GetOperand(0), value.Type));
            else if (value.LLVMValue.IsACallInst.Handle != IntPtr.Zero)
            {
                var tmp = Builder.BuildAlloca(value.Type.LLVMType);

                Builder.BuildStore(value.LLVMValue, tmp);
                Load(MugValue.From(tmp, value.Type));
            }
            else
                Load(value);
        }

        public void Exit()
        {
            if (Builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
                Builder.BuildBr(ExitBlock);
        }

        public void ExpectIndexerType(Range position)
        {
            if (!PeekType().MatchIntType())
                _generator.Report(position, $"'{PeekType()}' is not an indexer type");
        }

        public bool LoadReference(MugValue allocation, Range position)
        {
            if (allocation.IsConst)
            {
                _generator.Report(position, "Unable to take the address of a constant value");
                return false;
            }

            Load(MugValue.From(allocation.LLVMValue, MugValueType.Reference(allocation.Type)));
            return true;
        }

        public MugValue LoadFromPointer(MugValue value, Range position)
        {
            if (!value.Type.IsPointer())
                _generator.Report(position, "Expected a pointer");

            return MugValue.From(Builder.BuildLoad(value.LLVMValue), value.Type.PointerBaseElementType);
        }

        public void StoreInsidePointer(MugValue ptr)
        {
            Builder.BuildStore(Pop().LLVMValue, ptr.LLVMValue);
        }

        public void StoreElementArray(LLVMValueRef arrayload, int i)
        {
            var ptr = Builder.BuildGEP(arrayload, new[]
            {
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)i)
            });
            
            Builder.BuildStore(Pop().LLVMValue, ptr);
        }

        public void MakePostfixIntOperation(Func<LLVMValueRef, LLVMValueRef, string, LLVMValueRef> operation)
        {
            var target = Pop();
            var result = operation(Builder.BuildLoad(target.LLVMValue), LLVMValueRef.CreateConstInt(target.Type.LLVMType, 1), "");

            Builder.BuildStore(result, target.LLVMValue);
        }

        public void OperateInsidePointer(MugValue target)
        {
            Builder.BuildStore(Pop().LLVMValue, target.LLVMValue);
        }

        public void SelectArrayElement(bool buildload, MugValue array, MugValue index)
        {
            // selecting the element
            var ptr = Builder.BuildGEP( array.LLVMValue, new[] { index.LLVMValue });

            if (buildload) // load from pointer 
                ptr = Builder.BuildLoad(ptr);

            Load(MugValue.From(ptr, array.Type.ArrayBaseElementType));
        }

        public void Swap()
        {
            var second = Pop();
            var first = Pop();
            Load(second);
            Load(first);
        }

        public void AddFloat(MugValue left, MugValue right)
        {
            Load(
                MugValue.From(Builder.BuildFAdd(left.LLVMValue, right.LLVMValue), right.Type, isconstant: right.IsConstant && left.IsConstant));
        }

        public void SubFloat(MugValue left, MugValue right)
        {
            Load(MugValue.From(Builder.BuildFSub(left.LLVMValue, right.LLVMValue), right.Type, isconstant: right.IsConstant && left.IsConstant));
        }

        public void MulFloat(MugValue left, MugValue right)
        {
            Load(MugValue.From(Builder.BuildFMul(left.LLVMValue, right.LLVMValue), right.Type, isconstant: right.IsConstant && left.IsConstant));
        }

        public void DivFloat(MugValue left, MugValue right)
        {
            Load(MugValue.From(Builder.BuildFDiv(left.LLVMValue, right.LLVMValue), right.Type, isconstant: right.IsConstant && left.IsConstant));
        }

        public void NegFloat()
        {
            var value = Pop();
            Load(MugValue.From(Builder.BuildFNeg(value.LLVMValue), value.Type, isconstant: value.IsConstant));
        }

        public void CompareFloat(LLVMRealPredicate kind, MugValue left, MugValue right)
        {
            Load(MugValue.From(Builder.BuildFCmp(kind, left.LLVMValue, right.LLVMValue), MugValueType.Bool, isconstant: right.IsConstant && left.IsConstant));
        }

        public void CastFloat(MugValueType type)
        {
            var value = Pop();
            Load(
                MugValue.From(Builder.BuildFPCast(value.LLVMValue, type.LLVMType), type, isconstant: value.IsConstant));
        }

        public void CastFloatToInt(MugValueType type)
        {
            var value = Pop();
            Load(
                MugValue.From(Builder.BuildFPToSI(Pop().LLVMValue, type.LLVMType), type, isconstant: value.IsConstant));
        }

        public void ReallocMemory()
        {
            Memory = new Dictionary<string, MugValue>(Memory);
        }
    }
}
