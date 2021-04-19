using LLVMSharp;
using LLVMSharp.Interop;
using Mug.Compilation;
using Mug.Compilation.Symbols;
using Mug.Models.Generator.Emitter;
using Mug.Models.Lexer;
using Mug.Models.Lowerering;
using Mug.Models.Parser;
using Mug.Models.Parser.NodeKinds;
using Mug.Models.Parser.NodeKinds.Statements;
using Mug.MugValueSystem;
using Mug.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mug.Models.Generator
{
    public class LocalGenerator
    {
        // code emitter
        private MugEmitter _emitter;
        // function info
        private readonly FunctionNode _function;
        // pointers
        internal readonly IRGenerator _generator;

        private readonly LLVMValueRef _llvmfunction;
        private LLVMBasicBlockRef _oldcondition;
        private LLVMBasicBlockRef CycleExitBlock { get; set; }
        private LLVMBasicBlockRef CycleCompareBlock { get; set; }

        internal LocalGenerator(IRGenerator errorHandler, ref LLVMValueRef llvmfunction, ref FunctionNode function, ref MugEmitter emitter)
        {
            _generator = errorHandler;
            _emitter = emitter;
            _function = function;
            _llvmfunction = llvmfunction;
        }

        internal void Error(ModulePosition position, string error)
        {
            _generator.Parser.Lexer.Throw(position, error);
        }

        internal bool Report(ModulePosition position, string error)
        {
            _generator.Parser.Lexer.DiagnosticBag.Report(position, error);
            return false;
        }

        internal void Stop()
        {
            _generator.Parser.Lexer.CheckDiagnostic();
        }

        private LLVMValueRef FlatString(LLVMValueRef value, LLVMValueRef size, LLVMBuilderRef builder = default)
        {
            SetToEmitterWhenDefault(ref builder);

            return builder.BuildCall(GetUtilFunction("create_string"), new[] { value, size }, "^cll_create_str");
        }

        private LLVMValueRef CreateConstString(string value, bool cstring = true)
        {
            const int MAX_CONST_STRING_LEN = 1000;
            if (value.Length > MAX_CONST_STRING_LEN || !_generator.Table.ConstStringCache.TryGetValue(value, out var str))
                _generator.Table.ConstStringCache.Add(value, str = GepOF(_emitter.Builder.BuildGlobalString(value, "^cnst_str"), 0));

            if (cstring)
                return str;

            return FlatString(str, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (uint)value.Length));
        }

        /// <summary>
        /// converts a constant in token format to one in LLVMValueRef format
        /// </summary>
        internal MugValue ConstToMugConst(Token constant, ModulePosition position, bool isenum = false, MugValueType forcedIntSize = new())
        {
            LLVMValueRef llvmvalue = new();
            MugValueType type = new();

            switch (constant.Kind)
            {
                case TokenKind.ConstantDigit:
                    if (isenum)
                    {
                        llvmvalue = LLVMValueRef.CreateConstInt(forcedIntSize.GetLLVMType(_generator), Convert.ToUInt64(constant.Value));
                        type = forcedIntSize;
                    }
                    else
                    {
                        llvmvalue = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, Convert.ToUInt64(constant.Value));
                        type = MugValueType.Int32;
                    }
                    break;
                case TokenKind.ConstantBoolean:
                    llvmvalue = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, _generator.StringBoolToIntBool(constant.Value));
                    type = MugValueType.Bool;
                    break;
                case TokenKind.ConstantString:
                    llvmvalue = CreateConstString(constant.Value, false);
                    type = MugValueType.String;
                    break;
                case TokenKind.ConstantChar:
                    llvmvalue = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, _generator.StringCharToIntChar(constant.Value));
                    type = MugValueType.Char;
                    break;
                case TokenKind.ConstantFloatDigit:
                    llvmvalue = LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, double.Parse(constant.Value));
                    type = MugValueType.Float32;
                    break;
                default:
                    _generator.NotSupportedType<LLVMValueRef>(constant.Kind.ToString(), position);
                    break;
            }

            return MugValue.From(llvmvalue, type, isconstant: true);
        }

        private FunctionSymbol? OperatorFunctionSymbol(string name, ModulePosition position, ref MugValue[] parameters)
        {
            MugValue? basevalue = null;
            return GetFunctionSymbol(ref basevalue, name, Array.Empty<MugValueType>(), ref parameters, position);
        }

        /// <summary>
        /// calls the math operator or a user defined one
        /// </summary>
        /// <param name="name">operator to call if no built in operation matched</param>
        /// <param name="opint">built in operation to call if matched two int</param>
        /// <param name="opfloat">built in operation to call if matched two int</param>
        /// <param name="position">position for error</param>
        /// <returns></returns>
        private bool EmitMathOperator(string name, Action<MugValue, MugValue> opint, Action<MugValue, MugValue> opfloat, ModulePosition position)
        {
            _emitter.CoerceCoupleConstantSize();

            _emitter.GetCoupleValues(out var left, out var right);

            if (left.Type.MatchSameIntType(right.Type))
                opint(left, right);
            else if (right.Type.MatchSameFloatType(right.Type))
                opfloat(left, right);
            else
            {
                var parameters = new[] { left, right };
                return _emitter.CallOperator(OperatorFunctionSymbol(name, position, ref parameters), position, true, parameters);
            }

            return true;
        }


        private bool EmitSum(ModulePosition position)
        {
            return EmitMathOperator("+", _emitter.AddInt, _emitter.AddFloat, position);
        }

        private bool EmitSub(ModulePosition position)
        {
            return EmitMathOperator("-", _emitter.SubInt, _emitter.SubFloat, position);
        }

        private bool EmitMul(ModulePosition position)
        {
            return EmitMathOperator("*", _emitter.MulInt, _emitter.MulFloat, position);
        }

        private bool EmitDiv(ModulePosition position)
        {
            return EmitMathOperator("/", _emitter.DivInt, _emitter.DivFloat, position);
        }

        private LLVMRealPredicate ToFloatComparePredicate(LLVMIntPredicate intpredicate)
        {
            return intpredicate switch
            {
                LLVMIntPredicate.LLVMIntEQ => LLVMRealPredicate.LLVMRealOEQ,
                LLVMIntPredicate.LLVMIntNE => LLVMRealPredicate.LLVMRealONE,
                LLVMIntPredicate.LLVMIntSGE => LLVMRealPredicate.LLVMRealOGE,
                LLVMIntPredicate.LLVMIntSGT => LLVMRealPredicate.LLVMRealOGT,
                LLVMIntPredicate.LLVMIntSLE => LLVMRealPredicate.LLVMRealOLE,
                LLVMIntPredicate.LLVMIntSLT => LLVMRealPredicate.LLVMRealOLT
            };
        }

        private bool EmitBooleanOperator(string literal, LLVMIntPredicate llvmpredicate, TokenKind kind, ModulePosition position)
        {
            _emitter.CoerceCoupleConstantSize();

            _emitter.GetCoupleValues(out var left, out var right);
            var ft = left.Type;
            var st = right.Type;

            if ((kind == TokenKind.BooleanEQ || kind == TokenKind.BooleanNEQ) && ft.IsSameEnumOf(st))
            {
                /*if (_emitter.OneOfTwoIsOnlyTheEnumType(left, right))
                    return Report(position, "Cannot apply boolean operator on this expression");*/

                var enumBaseType = st.GetEnum().BaseType.ToMugValueType(_generator);

                _emitter.Load(MugValue.From(left.LLVMValue, enumBaseType));
                _emitter.Load(MugValue.From(right.LLVMValue, enumBaseType));

                return EmitBooleanOperator(literal, llvmpredicate, kind, position);
            }
            else if (ft.MatchSameAnyIntType(st)) // int == int (works for all low-level integers like chr and bool ..)
                _emitter.CompareInt(llvmpredicate, left, right);
            else if (ft.MatchSameFloatType(st)) // float == float
                _emitter.CompareFloat(ToFloatComparePredicate(llvmpredicate), left, right);
            else
            {
                var parameters = new[] { left, right };
                return _emitter.CallOperator(OperatorFunctionSymbol(literal, position, ref parameters), position, true, parameters); // else call an operator, this works also with int32 + int64 (bitness mismatch)
            }

            return true;
        }

        /// <summary>
        /// the function manages the operator implementations for all the types
        /// </summary>
        private bool EmitOperator(TokenKind kind, ModulePosition position)
        {
            return kind switch
            {
                TokenKind.Plus => EmitSum(position),
                TokenKind.Minus => EmitSub(position),
                TokenKind.Star => EmitMul(position),
                TokenKind.Slash => EmitDiv(position),
                TokenKind.BooleanEQ => EmitBooleanOperator("==", LLVMIntPredicate.LLVMIntEQ, kind, position),
                TokenKind.BooleanNEQ => EmitBooleanOperator("!=", LLVMIntPredicate.LLVMIntNE, kind, position),
                TokenKind.BooleanGreater => EmitBooleanOperator(">", LLVMIntPredicate.LLVMIntSGT, kind, position),
                TokenKind.BooleanGEQ => EmitBooleanOperator(">=", LLVMIntPredicate.LLVMIntSGE, kind, position),
                TokenKind.BooleanLess => EmitBooleanOperator("<", LLVMIntPredicate.LLVMIntSLT, kind, position),
                TokenKind.BooleanLEQ => EmitBooleanOperator("<=", LLVMIntPredicate.LLVMIntSLE, kind, position),
            };
        }

        private LLVMValueRef? tmp = null;

        private bool EmitAndOperator(INode left, INode right)
        {
            var tmpisnull = !tmp.HasValue;
            if (tmpisnull)
            {
                tmp = _emitter.Builder.BuildAlloca(LLVMTypeRef.Int1);
                _emitter.Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0), tmp.Value);
            }

            var iftrue = _llvmfunction.AppendBasicBlock("");
            var @finally = _llvmfunction.AppendBasicBlock("");

            if (!EvaluateExpression(left))
                return false;

            var f = _emitter.Peek();
            _generator.ExpectBoolType(f.Type, left.Position);

            _emitter.CompareJump(iftrue, @finally);
            _emitter.Builder.PositionAtEnd(iftrue);

            if (!EvaluateExpression(right))
                return false;

            var s = _emitter.Pop();
            _generator.ExpectBoolType(s.Type, right.Position);
            _emitter.Builder.BuildStore(s.LLVMValue, tmp.Value);

            _emitter.Jump(@finally);
            _emitter.Builder.PositionAtEnd(@finally);

            _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(tmp.Value), MugValueType.Bool));

            if (tmpisnull)
                tmp = null;

            return true;
        }

        private bool EmitOrOperator(INode left, INode right)
        {
            var tmpisnull = !tmp.HasValue;
            
            if (tmpisnull)
                tmp = _emitter.Builder.BuildAlloca(LLVMTypeRef.Int1);
            
            var iftrue = _llvmfunction.AppendBasicBlock("");
            var @finally = _llvmfunction.AppendBasicBlock("");

            if (!EvaluateExpression(left))
                return false;

            var f = _emitter.Peek();
            _generator.ExpectBoolType(f.Type, left.Position);
            _emitter.Builder.BuildStore(f.LLVMValue, tmp.Value);

            _emitter.CompareJump(@finally, iftrue);
            _emitter.Builder.PositionAtEnd(iftrue);

            if (!EvaluateExpression(right))
                return false;

            var s = _emitter.Pop();
            _generator.ExpectBoolType(s.Type, right.Position);
            _emitter.Builder.BuildStore(s.LLVMValue, tmp.Value);

            _emitter.Jump(@finally);
            _emitter.Builder.PositionAtEnd(@finally);

            _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(tmp.Value), MugValueType.Bool));

            if (tmpisnull)
                tmp = null;

            return true;
        }

        private static bool IsABitcast(MugValueType expressionType, MugValueType castType)
        {
            return
                (expressionType.TypeKind == MugValueTypeKind.String && castType.Equals(MugValueType.Array(MugValueType.Char))) ||
                (castType.TypeKind == MugValueTypeKind.String && expressionType.Equals(MugValueType.Array(MugValueType.Char)))   ||
                (castType.TypeKind == MugValueTypeKind.Reference && expressionType.Equals(MugValueType.Pointer(castType.PointerBaseElementType))) ||
                (expressionType.TypeKind == MugValueTypeKind.Reference && castType.Equals(MugValueType.Pointer(expressionType.PointerBaseElementType)));
        }

        LLVMValueRef GepOF(LLVMValueRef tmp, int index, LLVMBuilderRef builder = default)
        {
            SetToEmitterWhenDefault(ref builder);

            return builder.BuildStructGEP(tmp, (uint)index);
        }

        private LLVMValueRef BoxValue(MugValue value, int index, LLVMTypeRef biggesttype)
        {
            var tmp = _emitter.Builder.BuildAlloca( // tag and type
                LLVMTypeRef.CreateStruct(new[] { LLVMTypeRef.Int8, biggesttype }, true));

            _emitter.Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, (uint)index), GepOF(tmp, 0));

            _emitter.Builder.BuildStore(
                value.LLVMValue,
                _emitter.Builder.BuildBitCast(
                    GepOF(tmp, 1), LLVMTypeRef.CreatePointer(value.Type.GetLLVMType(_generator), 0))
                );

            return _emitter.Builder.BuildLoad(tmp);
        }

        /// <summary>
        /// the function manages the 'as' operator
        /// </summary>
        private bool EmitCastInstruction(MugType type, ModulePosition position)
        {
            // the expression type to cast
            var expressionType = _emitter.PeekType();
            var castType = type.ToMugValueType(_generator);

            if (castType.RawEquals(expressionType))
                return Report(position, "Useless cast");

            if (castType.IsEnum())
            {
                var enumerated = castType.GetEnum();
                var enumBaseType = enumerated.BaseType.ToMugValueType(_generator);

                _emitter.CoerceConstantSizeTo(enumBaseType);

                if (_emitter.PeekType().TypeKind != enumBaseType.TypeKind)
                    return Report(position, $"The base type of enum '{enumerated.Name}' is incompatible with type '{expressionType}'");

                _emitter.CastToEnumMemberFromBaseType(castType);
            }
            else if (expressionType.TypeKind == MugValueTypeKind.Enum)
            {
                var enumBaseType = expressionType.GetEnum().BaseType.ToMugValueType(_generator);

                _emitter.CoerceConstantSizeTo(enumBaseType);

                if (_emitter.PeekType().GetEnum().BaseType.ToMugValueType(_generator).TypeKind != castType.TypeKind)
                    return Report(type.Position, $"Enum base type is incompatible with type '{castType}'");

                _emitter.CastEnumMemberToBaseType(castType);
            }
            else if (IsABitcast(expressionType, castType))
            {
                var value = _emitter.Pop();
                value.Type = castType;
                _emitter.Load(value);
            }
            else if (castType.TypeKind == MugValueTypeKind.Unknown || expressionType.TypeKind == MugValueTypeKind.Unknown)
            {
                var value = _emitter.Pop();

                if (!value.Type.IsPointer() && !value.Type.IsIndexable())
                    return Report(position, "Expected pointer when in cast expression something is unknown");
                
                _emitter.Load(
                    MugValue.From(_emitter.Builder.BuildBitCast(value.LLVMValue, castType.GetLLVMType(_generator)),
                    castType));
            }
            else if (castType.IsVariant())
            {
                var variant = castType.GetVariant();
                var index = variant.Body.FindIndex(t => t.ToMugValueType(_generator).Equals(expressionType));

                if (index == -1)
                    return Report(position, $"Variant type '{variant.Name}' does not include '{expressionType}', the boxing is impossible");

                _emitter.Load(
                    MugValue.Struct(
                        BoxValue(_emitter.Pop(), index, _generator.GetBiggestTypeOFVariant(variant).GetLLVMType(_generator)),
                        MugValueType.Variant(variant))
                    );
            }
            else if (expressionType.MatchAnyIntType() && castType.MatchAnyIntType())
                _emitter.CastInt(castType);
            else if (expressionType.MatchIntType() && castType.MatchFloatType())
                _emitter.CastIntToFloat(castType);
            else if (expressionType.MatchFloatType() && castType.MatchFloatType())
                _emitter.CastFloat(castType);
            else if (expressionType.MatchFloatType() && castType.MatchIntType())
                _emitter.CastFloatToInt(castType);
            else
                return _emitter.CallAsOperator(position, expressionType, type.ToMugValueType(_generator));

            return true;
        }

        /*/// <summary>
        /// the function evaluates an instance node, for example: base.method()
        /// </summary>
        private bool EvaluateMemberAccess(INode member, bool load)
        {
            switch (member)
            {
                case MemberNode m:
                    if (!EvaluateMemberAccess(m.Base, load))
                        return false;

                    var structure = _emitter.PeekType().GetStructure();
                    _emitter.LoadField(
                        _emitter.Pop(),
                        structure.GetFieldTypeFromName(m.Member.Value, _generator, m.Member.Position),
                        structure.GetFieldIndexFromName(m.Member.Value, _generator, m.Member.Position), load);
                    break;
                case Token t:
                    return load ? !_emitter.LoadFromMemory(t.Value, t.Position, load) : !_emitter.LoadMemoryAllocation(t.Value, t.Position);
                case ArraySelectElemNode a:
                    return EmitExprArrayElemSelect(a);
                case PrefixOperator p:
                    if (!EvaluateMemberAccess(p.Expression, p.Prefix == TokenKind.Star))
                        return false;

                    if (p.Prefix == TokenKind.Star)
                        _emitter.Load(_emitter.LoadFromPointer(_emitter.Pop(), p.Position));
                    else if (p.Prefix == TokenKind.BooleanAND)
                    {
                        if (!EvaluateMemberAccess(p.Expression, load))
                            return false;

                        if (!_emitter.LoadReference(_emitter.Pop(), p.Position))
                            return false;
                    }
                    else
                        return Report(p.Position, "In member access, the base must be a non-expression");

                    break;
                default:
                    Error(member.Position, "Not supported yet");
                    break;
            }

            return true;
        }*/

        private FunctionSymbol? EvaluateFunctionCallName(INode leftexpression, ref MugValue[] parameters, MugValueType[] genericsInput, out MugValue? basetype)
        {
            string name;
            ModulePosition position;
            basetype = null;

            if (leftexpression is Token token)
            {
                if (token.Kind != TokenKind.Identifier)
                {
                    Report(token.Position, "Uncallable item");
                    return null;
                }

                name = token.Value;
                position = token.Position;
            }
            else if (leftexpression is MemberNode member) // (expr).function()
            {
                if (!EvaluateExpression(member.Base))
                    return null;

                basetype = _emitter.Pop();
                name = member.Member.Value;
                position = member.Member.Position;
            }
            else // (expr)()
            {
                Error(leftexpression.Position, "Currently unsupported");
                // fix when implemented function pointers
                EvaluateExpression(leftexpression);
                throw new(); // tofix
            }

            return GetFunctionSymbol(ref basetype, name, genericsInput, ref parameters, position);
        }

        private List<FunctionSymbol> EvaluateGenericFunctionOverloads(List<FunctionNode> overloads, MugValueType[] generics)
        {
            var result = new List<FunctionSymbol>();
            var oldgenerics = _generator.GenericParameters;
            _generator.GenericParameters = new();

            foreach (var function in overloads)
            {
                FunctionSymbol functionIdentifier = new();
                if (function.Generics.Count == generics.Length)
                {
                    _generator.AddGenericParameters(function.Generics, generics);

                    // the function will be evaluated later to avoid the evaluation of all the functions also if unused
                    functionIdentifier = new FunctionSymbol(
                        function.Base?.Type.ToMugValueType(_generator),
                        Array.Empty<MugValueType>(),
                        _generator.ParameterTypesToMugTypes(function.ParameterList.Parameters),
                        function.ReturnType.ToMugValueType(_generator), new(), function.Position);

                    _generator.ClearGenericParameters();
                }

                result.Add(functionIdentifier);
            }

            _generator.GenericParameters = oldgenerics;

            return result;
        }

        private FunctionSymbol? GetFunctionSymbol(
            ref MugValue? basevalue,
            string name,
            MugValueType[] generics,
            ref MugValue[] parameters,
            ModulePosition position)
        {
            if (!_generator.Table.DefinedFunctions.ContainsKey(name) && !_generator.GenerateOverloadsOF(name, generics))
                // if the table does not contain a definition for the function's name we report it
                Report(position, $"Undeclared function '{name}'");

            // todo: cache generated generic function
            List<FunctionSymbol> overloads;
            List<FunctionNode> genericOverloads = null;

            // if searching for a generic structure
            if (generics.Length > 0)
                overloads = EvaluateGenericFunctionOverloads(genericOverloads = _generator.Table.GetOverloadsOFGenericFunction(name), generics);
            else if (!_generator.Table.DefinedFunctions.TryGetValue(name, out overloads))
            {
                // if the table does not contain a definition for the function's name we report it
                Report(position, $"Undeclared function '{name}'");
                return null;
            }

            // we iterate over the overloads' list to find a valid one
            for (int j = 0; j < overloads.Count; j++)
            {
                var function = overloads[j];

                // parameters are null when then function has diffent generics number from 'generics' (this is a side effect caused by EvaluateGenericFunctionOverloads)
                if (function.Parameters is null ||
                    parameters.Length != function.Parameters.Length || basevalue.HasValue != function.BaseType.HasValue)
                    continue;

                if (basevalue.HasValue)
                {
                    _emitter.Load(basevalue.Value);
                    _emitter.CoerceConstantSizeTo(function.BaseType.Value);

                    basevalue = _emitter.Pop();
                    if (!basevalue.Value.Type.Equals(function.BaseType.Value))
                        continue;
                }

                for (int i = 0; i < function.Parameters.Length; i++)
                {
                    // using the stack utilities to make bitness coersions with constants
                    _emitter.Load(parameters[i]);
                    _emitter.CoerceConstantSizeTo(function.Parameters[i]);

                    // value is useless on the stack so we can pop it
                    if (!function.Parameters[i].Equals(_emitter.PeekType()))
                    {
                        // removing the value from the top
                        _emitter.Pop();
                        goto mismatch;
                    }
                    else
                        parameters[i] = _emitter.Pop();
                }

                if (generics.Length > 0)
                {
                    // if the generic function is already cached we use it to save time
                    if (_generator.Table.GetGenericFunctionSymbol(genericOverloads[j].Name, function.BaseType, function.Parameters, function.ReturnType, out var symbol))
                        function.Value = symbol.Value;
                    else // otherwise we evaluate it and we cache it to save time for the next use
                    {
                        var llvmprototype = _generator.GetLLVMPrototype(genericOverloads[j], generics);
                        function.Value = MugValue.From(
                            _generator.EvaluateFunction(
                                genericOverloads[j],
                                llvmprototype.LLVMValue,
                                generics),
                            llvmprototype.Type);

                        _generator.Table.DeclareGenericFunctionSymbol(genericOverloads[j].Name, function);
                    }
                }

                return function;
            mismatch:;
            }

            // could not find function
            var types = new MugValueType[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                types[i] = parameters[i].Type;
            
            Report(position, $"No overload of function '{name}' accepts {(parameters.Length > 0 ? $"'{string.Join("', '", types)}' as parameter{IRGenerator.GetPlural(parameters.Length)}" : "no parameters")}{(basevalue.HasValue ? $" and with '{basevalue.Value.Type}' as base type" : "")}");
            return null;
        }

        private static bool IsEntryPoint(CallStatement c)
        {
            return
                c.Name is Token t &&
                t.Value == IRGenerator.EntryPointName &&
                c.Generics.Count == 0 &&
                c.Parameters.Nodes.Count == 0 &&
                c.IsBuiltIn == false;
        }

        /// <summary>
        /// the function converts a Callstatement node to the corresponding low-level code
        /// </summary>
        private bool EmitCallStatement(CallStatement c, bool expectedNonVoid, bool isInCatch = false)
        {
            // to replace with built in macros
            if (c.IsBuiltIn)
                return EmitBuiltIn(c.Name, c.Generics, c.Parameters.Nodes.ToArray(), expectedNonVoid, c.Position);

            if (IsEntryPoint(c))
                return Report(c.Position, "Entry point cannot be called");

            // an array is prepared for the parameter types of function to call
            var parameters = new MugValue[c.Parameters.Nodes.Count];
            var paramsAreOk = true;

            // here you can infer the generic type

            /* the array is cycled with the expressions of the respective parameters and each expression
             * is evaluated and assigned its type to the array of parameter types
             */
            for (int i = 0; i < c.Parameters.Nodes.Count; i++)
                if (paramsAreOk &= EvaluateExpression(c.Parameters.Nodes[i]))
                    parameters[i] = _emitter.Pop();

            /*
             * the symbol of the function is taken by passing the name of the complete function which consists
             * of the function id and in brackets the list of parameter types separated by ', '
             */
            
            if (!paramsAreOk)
                return false;

            // passing as ref to get result of constant bitness coercion
            var function = EvaluateFunctionCallName(c.Name, ref parameters, _generator.MugTypesToMugValueTypes(c.Generics), out var basevalue);

            if (function is null)
                return false;

            // function type: <ret_type> <param_types>
            var functionType = function?.ReturnType;

            if (expectedNonVoid)
                _generator.ExpectNonVoidType(functionType.Value.GetLLVMType(_generator), c.Position);

            _emitter.Call(function.Value.Value.LLVMValue, parameters, functionType.Value, basevalue);

            if (!isInCatch && functionType.Value.TypeKind == MugValueTypeKind.EnumError)
                return Report(c.Position, "Uncatched enum error");
            else if (isInCatch && functionType.Value.TypeKind != MugValueTypeKind.EnumError)
                return Report(c.Position, "Catched a non enum error");

            return true;
        }

        internal void SetUpGlobals()
        {
            var int64_1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1);
            var int64_0 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0);
            var strtype = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);

            var open = _generator.RequireFunction(
                "_fdopen",
                // file stream
                strtype, // return type
                LLVMTypeRef.Int64,
                strtype);

            var stdout = _emitter.Builder.BuildCall(open, new[] { int64_1, CreateConstString("w") });
            var stdin = _emitter.Builder.BuildCall(open, new[] { int64_0, CreateConstString("r") });
            var args = CreateHeapArray(MugValueType.Array(MugValueType.CString), _llvmfunction.GetParam(0), false); // called only when main, so params will ever be argc and argv
            _emitter.Builder.BuildStore(_llvmfunction.GetParam(1), GepOF(args, 1));

            _emitter.Builder.BuildStore(stdout, _generator.Module.GetNamedGlobal("@stdout"));
            _emitter.Builder.BuildStore(stdin, _generator.Module.GetNamedGlobal("@stdin"));
            _emitter.Builder.BuildStore(args, _generator.Module.GetNamedGlobal("@args"));
        }

        internal void EndGlobals()
        {
            var open = _generator.RequireFunction(
                "fclose",
                // file stream
                LLVMTypeRef.Int64,
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));

            if (_llvmfunction.LastBasicBlock.Terminator.Handle != IntPtr.Zero)
            {
                unsafe { _emitter.Builder = LLVM.CreateBuilder(); }
                _emitter.Builder.PositionBefore(_llvmfunction.LastBasicBlock.Terminator);
            }
            var stdout = _emitter.Builder.BuildLoad(_generator.Module.GetNamedGlobal("@stdout"));
            var stdin = _emitter.Builder.BuildLoad(_generator.Module.GetNamedGlobal("@stdin"));

            _emitter.Builder.BuildCall(open, new[] { stdout });
            _emitter.Builder.BuildCall(open, new[] { stdin });
        }

        private bool CompTime_sizeof(MugType generic)
        {
            var size = generic.ToMugValueType(_generator).Size(_generator.SizeOfPointer, _generator);

            _emitter.Load(MugValue.From(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)size), MugValueType.Int32, isconstant: true));
            return true;
        }

        private bool CompTime_box_kind(INode expression)
        {
            if (!EvaluateExpression(expression))
                return false;

            _emitter.MakeTempAllocation();
            
            var box = _emitter.Pop();

            if (!box.Type.IsVariant())
                return Report(expression.Position, "Required argument of type variant");
            
            var kind = _emitter.Builder.BuildIntCast(_emitter.Builder.BuildLoad(GepOF(box.LLVMValue, 0)), LLVMTypeRef.Int32);

            _emitter.Load(MugValue.From(kind, MugValueType.Int32, isconstant: true));
            return true;
        }

        private static ArrayAllocationNode GetArrayFromVarArgs(INode[] parameters)
        {
            var args = new INode[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                args[i] = new CallStatement()
                {
                    Position = parameters[i].Position,
                    IsBuiltIn = true,
                    Name = new Token(TokenKind.Identifier, "str", parameters[i].Position, false),
                    Parameters = new NodeBuilder() { Nodes = new() { parameters[i] } },
                };

            return new ArrayAllocationNode()
            {
                Position = parameters.First().Position,
                Body = args.ToList(),
                Type = new MugType(parameters.First().Position, TypeKind.String),
                SizeIsImplicit = true
            };
        }

        private bool CheckFormat(string format, int argsCount, ModulePosition position)
        {
            var errorsCount = _generator.Parser.Lexer.DiagnosticBag.Count;
            var i = 0;
            var formatsCount = 0;

            bool hasNext() => i + 1 < format.Length;

            ModulePosition atChar(int count = 0, int endCount = 1) => new(position.Lexer, (position.Position.Start.Value + 1 + i + count)..(position.Position.Start.Value + 1 + i + count  + endCount));

            for (; i < format.Length; i++)
            {
                var c = format[i];

                if (c == '}')
                {
                    if (hasNext() && format[i + 1] == '}')
                        i++;
                    else
                        Report(atChar(), "Unexpected '}'");
                }
                else if (c == '{')
                {
                    if (!hasNext())
                    {
                        Report(atChar(), "Needed another '{', or '}'");
                        continue;
                    }
                    
                    var next = format[i + 1];
                    if (next == '{')
                        i++;
                    else if (next == '}')
                    {
                        i++;
                        formatsCount++;
                    }
                    else
                    {
                        var nextIsControl = char.IsControl(next);
                        Report(atChar(1, 1 + Convert.ToInt32(nextIsControl)), $"Unexpected '{next.ToString().Replace("\n", "\\n")}', needed '}}'");
                    }
                }
            }

            if (formatsCount != argsCount)
                Report(position, $"Passed '{argsCount}' argument{IRGenerator.GetPlural(argsCount)}, referred '{formatsCount}'");

            return errorsCount == _generator.Parser.Lexer.DiagnosticBag.Count;
        }

        private bool FormatString(ref INode[] parameters, out Token result)
        {
            result = default;

            var format = parameters.First();
            parameters = parameters[1..]; // popping the formatter

            if (format is not Token fmt || fmt.Kind != TokenKind.ConstantString)
                return Report(format.Position, "Expected a constant string to format");

            result = fmt;
            return CheckFormat(fmt.Value, parameters.Length, fmt.Position);
        }

        private bool CompTime_fmt(INode[] parameters, ModulePosition position)
        {
            if (!FormatString(ref parameters, out var fmt) || parameters.Length == 0 || !EmitExprAllocateArray(GetArrayFromVarArgs(parameters)))
                return false;

            var args = _emitter.Pop();

            unsafe
            {
                _emitter.Load(
                    MugValue.From(
                        _emitter.Builder.BuildCall(
                            GetUtilFunction("fmt"), new[]
                            {
                                CreateConstString(fmt.Value),
                                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)fmt.Value.Length),
                                _emitter.Builder.BuildBitCast(
                                    args.LLVMValue,
                                    LLVMTypeRef.CreatePointer(LLVM.GetTypeByName(_generator.Module, new MarshaledString("struct.arr").Value), 0), "^bltn_btcst")
                            }),
                        MugValueType.String)
                    );
            }
            return true;
        }

        private LLVMValueRef GetStdOut()
        {
            return _emitter.Builder.BuildLoad(_generator.Module.GetNamedGlobal("@stdout"));
        }

        private LLVMValueRef GetStdIn(LLVMBuilderRef builder = default)
        {
            SetToEmitterWhenDefault(ref builder);

            return builder.BuildLoad(_generator.Module.GetNamedGlobal("@stdin"));
        }

        private LLVMValueRef GetArgs()
        {
            return _emitter.Builder.BuildLoad(_generator.Module.GetNamedGlobal("@args"));
        }

        private void SetToEmitterWhenDefault(ref LLVMBuilderRef builder)
        {
            if (builder.Handle == IntPtr.Zero)
                builder = _emitter.Builder;
        }

        private bool CompTime_cstr(INode parameter, ModulePosition position)
        {
            if (parameter is Token t && t.Kind == TokenKind.ConstantString)
                _emitter.Load(MugValue.From(CreateConstString(t.Value), MugValueType.CString));
            else
            {
                if (!EvaluateExpression(parameter))
                    return false;

                var value = _emitter.Pop();
                if (value.Type.TypeKind != MugValueTypeKind.String)
                    return Report(position, "Required a parameter of type 'str'");

                LoadCStr(value);
            }

            return true;
        }

        private bool CompTime_carr(INode parameter, ModulePosition position)
        {
            if (!EvaluateExpression(parameter))
                return false;

            var value = _emitter.Pop();
            if (value.Type.TypeKind != MugValueTypeKind.Array)
                return Report(position, "Required a value of type array as parameter");

            LoadCArr(value);

            return true;
        }

        private void LoadCStr(MugValue str)
        {
            _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(GepOF(str.LLVMValue, 1)), MugValueType.CString));
        }

        private void LoadCArr(MugValue arr)
        {
            _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(GepOF(arr.LLVMValue, 1)), MugValueType.Pointer(arr.Type.ArrayBaseElementType)));
        }

        private bool CompTime_print(INode[] parameters, ModulePosition position)
        {
            if (parameters.Length == 0)
                return false;

            if (parameters.Length == 1)
            {
                if (!FormatString(ref parameters, out var fmt))
                    return false;

                _emitter.Load(MugValue.From(CreateConstString(fmt.Value.Replace("{{", "{").Replace("}}", "}"), false), MugValueType.String, isconstant: true));
            }
            else if (!CompTime_fmt(parameters, position))
                return false;

            var formatted = _emitter.Pop();

            var write = _generator.RequireFunction(
                "fwrite", // name
                LLVMTypeRef.Int64, // return type
                // buf
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
                // size, count
                LLVMTypeRef.Int64, LLVMTypeRef.Int64,
                // file stream
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)); // arg types

            var flush = _generator.RequireFunction(
                "fflush",
                LLVMTypeRef.Int32, // return type
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));

            var stdout = GetStdOut();

            var len = _emitter.Builder.BuildLoad(GepOF(formatted.LLVMValue, 0));
            LoadCStr(formatted);
            var cstrformat = _emitter.Pop().LLVMValue;
            var int64_1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1);

            _emitter.Builder.BuildCall(write, new[]
            {
                cstrformat,
                int64_1,
                len,
                stdout
            });

            _emitter.Builder.BuildCall(flush, new[] { stdout });
            return true;
        }

        private LLVMValueRef GetInputFunctionImpl()
        {
            const string name = "^input_impl";
            var func = _generator.Module.GetNamedFunction(name);
            if (func.Handle != IntPtr.Zero)
                return func;

            var function = LLVMTypeRef.CreateFunction(
                MugValueType.String.GetLLVMType(_generator), Array.Empty<LLVMTypeRef>());

            func = _generator.Module.AddFunction(name, function);

            var entry = func.AppendBasicBlock("");
            unsafe
            {
                var builder = (LLVMBuilderRef)LLVM.CreateBuilder();
                builder.PositionAtEnd(entry);

                var strtype = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
                var size = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 100);
                var buffer = GepOF(builder.BuildAlloca(LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, 100)), 0, builder);

                var gets = _generator.RequireFunction(
                    "fgets",
                    strtype, // return type
                    strtype,
                    LLVMTypeRef.Int32,
                    strtype);

                var strlen = _generator.RequireFunction(
                    "strlen",
                    LLVMTypeRef.Int64, // return type
                    strtype);

                var result = builder.BuildCall(gets, new[]
                {
                    buffer,
                    size,
                    GetStdIn(builder)
                });

                var len = builder.BuildCall(strlen, new[] { result });

                var int64_1 = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 1);

                var value = FlatString(result, builder.BuildSub(len, int64_1), builder);

                builder.BuildRet(value);
            }

            return func;
        }

        private bool CompTime_input(INode[] parameters, ModulePosition position)
        {
            if (parameters.Length > 0 && !CompTime_print(parameters, position))
                return false;

            var result = _emitter.Builder.BuildCall(GetInputFunctionImpl(), Array.Empty<LLVMValueRef>());

            _emitter.Load(MugValue.From(result, MugValueType.String));
            return true;
        }

        private LLVMValueRef GetUtilFunction(string name)
        {
            return _generator.Module.GetNamedFunction($"${name}");
        }

        private LLVMValueRef EmitReprCall(ref List<MugValue> parameters)
        {
            var value = parameters.First();
            switch (value.Type.TypeKind)
            {
                case MugValueTypeKind.Unknown:
                case MugValueTypeKind.Float32:
                case MugValueTypeKind.Float64:
                case MugValueTypeKind.Int8:
                case MugValueTypeKind.Int32:
                case MugValueTypeKind.Int64:
                case MugValueTypeKind.Bool:
                    return GetUtilFunction($"repr_{value.Type}");
                case MugValueTypeKind.Char:
                case MugValueTypeKind.String:
                    return GetUtilFunction($"quote_{value.Type}");
                case MugValueTypeKind.Pointer:
                    return GetUtilFunction("star");
                case MugValueTypeKind.Struct:
                    return GetUtilFunction("repr_struct");
                case MugValueTypeKind.Float128:
                    _emitter.Load(value);
                    _emitter.CastFloat(MugValueType.Float64);
                    parameters[0] = _emitter.Pop();
                    return GetUtilFunction("repr_f64");
                case MugValueTypeKind.Enum:
                    break;
                case MugValueTypeKind.Array:
                    var subparameters = new List<MugValue>() { MugValue.From(value.LLVMValue, value.Type.ArrayBaseElementType) };
                    parameters.Add(MugValue.From(EmitReprCall(ref subparameters), MugValueType.Undefinied));
                    return GetUtilFunction("repr_arr");
                case MugValueTypeKind.EnumError:
                    break;
                case MugValueTypeKind.Reference:
                    break;
                case MugValueTypeKind.Variant:
                    break;
                default:
                    throw new();
            }

            throw new();
        }

        private bool CompTime_cfg(INode parameter, ModulePosition position)
        {
            if (parameter is not Token sym || sym.Kind != TokenKind.Identifier)
                return Report(position, "Required a symbol name");

            _emitter.Load(
                MugValue.From(
                    LLVMValueRef.CreateConstInt(
                        LLVMTypeRef.Int1,
                        Convert.ToUInt32(_generator.Table.CompilerSymbolIsDeclared(sym.Value))),
                    MugValueType.Bool)
                );

            return true;
        }

        private bool CompTime_repr(INode parameter, ModulePosition position, bool quoteString)
        {
            if (!EvaluateExpression(parameter))
                return false;

            if (!quoteString && _emitter.PeekType().IsQuotable())
            {
                if (_emitter.PeekType().TypeKind == MugValueTypeKind.Char)
                    _emitter.Call(GetUtilFunction("chr_to_str"), new[] { _emitter.Pop() }, MugValueType.String, null);

                return true;
            }

            var parameters = new List<MugValue>() { _emitter.Pop() };
            var func = EmitReprCall(ref parameters);

            _emitter.Load(
                MugValue.From(
                    _emitter.Builder.BuildCall(func, _generator.MugValuesToLLVMValues(parameters.ToArray())),
                    MugValueType.String)
                );
            return true;
        }

        private bool CompTime_args()
        {
            _emitter.Load(
                MugValue.From(
                    GetArgs(),
                    MugValueType.Array(MugValueType.CString))
                );

            return true;
        }

        private bool CompTime_panic(INode[] parameters, ModulePosition position)
        {
            if (parameters.Length == 0)
                parameters = new[] { (INode)new Token(TokenKind.ConstantString, "", position, false) };

            var fmt = (Token)parameters[0];
            fmt.Value = $"panic! at {position.Lexer.ModuleRelativePath}:'{_function.Name}':{position.LineAt()}{(fmt.Value != "" ? $":\n{fmt.Value}" : "")}";
            parameters[0] = fmt;

            if (!CompTime_print(parameters, position))
                return false;

            var exit = _generator.RequireFunction("exit", LLVMTypeRef.Void, LLVMTypeRef.Int32);

            _emitter.Builder.BuildCall(exit, new[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1) });
            return true;
        }

        private bool CompTime_frm_str(INode parameter, ModulePosition position)
        {
            if (!EvaluateExpression(parameter))
                return false;

            var value = _emitter.Pop();
            if (!value.Type.IsCString())
                return Report(position, "Required a parameter of type '*chr'");

            var strlen = _generator.RequireFunction(
                "strlen",
                LLVMTypeRef.Int64,
                LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));

            _emitter.Load(
                MugValue.From(
                    FlatString(value.LLVMValue, _emitter.Builder.BuildCall(strlen, new[] { value.LLVMValue }, "^gt_str_ln")),
                    MugValueType.String)
                );
            return true;
        }

        private bool EmitBuiltIn(INode name, List<MugType> generics, INode[] parameters, bool expectedNonVoid, ModulePosition position)
        {
            if (name is not Token id)
                return Report(position, "Built in functions have no base");

            switch (id.Value)
            {
                case "size":
                    reportWhenUseless();
                    checkOverload(generics.Count == 1, parameters.Length == 0);
                    return CompTime_sizeof(generics.First());
                case "unbox":
                    reportWhenUseless();
                    checkOverload(generics.Count == 1, parameters.Length == 1);
                    return CompTime_unbox(generics.First(), parameters.First(), position);
                case "knd":
                    reportWhenUseless();
                    checkOverload(generics.Count == 0, parameters.Length == 1);
                    return CompTime_box_kind(parameters.First());
                case "fmt":
                    reportWhenUseless();
                    checkOverload(generics.Count == 0, parameters.Length > 1);
                    return CompTime_fmt(parameters, position);
                case "cfg":
                    reportWhenUseless();
                    checkOverload(generics.Count == 0, parameters.Length == 1);
                    return CompTime_cfg(parameters.First(), position);
                case "print":
                    reportWhenInExpression();
                    checkOverload(generics.Count == 0, parameters.Length > 0);
                    return CompTime_print(parameters, position);
                case "input":
                    checkOverload(generics.Count == 0);
                    return CompTime_input(parameters, position);
                case "carr" or "cstr":
                    reportWhenUseless();
                    checkOverload(generics.Count == 0, parameters.Length == 1);
                    var func = id.Value == "cstr" ? (Func<INode, ModulePosition, bool>)CompTime_cstr : CompTime_carr;
                    return func(parameters.First(), position);
                case "args":
                    reportWhenUseless();
                    checkOverload(generics.Count == 0, parameters.Length == 0);
                    return CompTime_args();
                case "frm_cstr":
                    reportWhenUseless();
                    checkOverload(generics.Count == 0, parameters.Length == 1);
                    return CompTime_frm_str(parameters.First(), position);
                case "repr" or "str":
                    reportWhenUseless();
                    checkOverload(generics.Count == 0, parameters.Length == 1);
                    return CompTime_repr(parameters.First(), position, id.Value == "repr");
                case "panic":
                    checkOverload(generics.Count == 0);
                    return CompTime_panic(parameters, position);
                default:
                    return Report(position, $"Unknown built in function '{id.Value}'");
            }

            void reportWhenInExpression()
            {
                if (expectedNonVoid)
                    Report(position, "Void expression");
            }

            void reportWhenUseless()
            {
                if (!expectedNonVoid)
                    Report(position, "Useless here");
            }

            void checkOverload(bool genericsCheck = true, bool paramsCheck = true)
            {
                if (!genericsCheck)
                    Error(position, $"Unexpected '{generics.Count}' generic parameter{IRGenerator.GetPlural(generics.Count)}");
                if (!paramsCheck)
                    Error(position, $"Unexpected '{parameters.Length}' parameter{IRGenerator.GetPlural(parameters.Length)}");
            }
        }

        private bool CompTime_unbox(MugType type, INode expression, ModulePosition position)
        {
            if (!EvaluateExpression(expression))
                return false;

            _emitter.MakeTempAllocation();

            var expr = _emitter.Pop();
            var expressionType = expr.Type;
            var castType = type.ToMugValueType(_generator);

            if (!expressionType.IsVariant())
                return Report(expression.Position, $"Required argument of type variant");

            var boxtype = expressionType.GetVariant();
            var index = boxtype.Body.FindIndex(type => type.ToMugValueType(_generator).RawEquals(castType));
            if (index == -1)
                return Report(position, $"Variant type '{boxtype.Name}' does not include type '{castType}'");

            _emitter.Load(UnboxValue(expr, boxtype, castType));

            return true;
        }

        private bool EmitExprPrefixOperator(PrefixOperator p)
        {
            if (p.Prefix == TokenKind.BooleanAND) // &x reference
                return _emitter.LoadReference(EvaluateLeftValue(p.Expression, false), p.Position);

            if (!EvaluateExpression(p.Expression))
                return false;

            if (p.Prefix == TokenKind.Negation) // '!' operator
            {
                _generator.ExpectBoolType(_emitter.PeekType(), p.Position);
                _emitter.NegBool();
            }
            else if (p.Prefix == TokenKind.Minus) // '-' operator, for example -(9+2) or -8+2
            {
                if (_emitter.PeekType().MatchIntType())
                    _emitter.NegInt();
                else if (_emitter.PeekType().MatchFloatType())
                    _emitter.NegFloat();
                else
                    Error(p.Position, $"Unable to perform operator '-' on type {_emitter.PeekType()}");
            }
            else if (p.Prefix == TokenKind.Star)
                _emitter.Load(_emitter.LoadFromPointer(_emitter.Pop(), p.Position));
            else if (p.Prefix == TokenKind.OperatorIncrement || p.Prefix == TokenKind.OperatorDecrement)
            {
                var left = EvaluateLeftValue(p.Expression);

                EmitPostfixOperator(left, p.Prefix, p.Position, false);

                _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(left.LLVMValue), left.Type));
            }

            return true;
        }

        private bool EmitExpr(ExpressionNode e)
        {
            // evaluated left and right, | allows to find bugs also in the right expression
            if (!EvaluateExpression(e.Left) | !EvaluateExpression(e.Right))
                return false;

            // operator implementation
            return EmitOperator(e.Operator, e.Position);
        }

        private bool EmitExprBool(BooleanExpressionNode b)
        {
            if (b.Operator == TokenKind.BooleanAND)
                return EmitAndOperator(b.Left, b.Right);
            else if (b.Operator == TokenKind.BooleanOR)
                return EmitOrOperator(b.Left, b.Right);
            else if (b.Operator == TokenKind.KeyIs)
                return EmitIsInstruction(b.Left, b.IsInstructionType, b.IsInstructionAlias, b.Position);
            else
            {
                if (!EvaluateExpression(b.Left) | !EvaluateExpression(b.Right))
                    return false;

                return EmitOperator(b.Operator, b.Position);
            }
        }

        public bool EmitIsInstruction(INode left, MugType right, Token? alias, ModulePosition position)
        {
            if (!EvaluateExpression(left))
                return false;

            _emitter.MakeTempAllocation();
            var value = _emitter.Pop();
            
            if (!value.Type.IsVariant())
                return Report(position, "Unable to perform 'is' operator over a non-boxed value");

            var righttype = right.ToMugValueType(_generator);
            var boxtype = value.Type.GetVariant();
            var index = boxtype.Body.FindIndex(type => type.ToMugValueType(_generator).RawEquals(righttype));
            if (index == -1)
                return Report(position, $"Variant type '{boxtype.Name}' does not include type '{righttype}'");

            if (!value.IsAllocaInstruction())
            {
                var tmp = _emitter.Builder.BuildAlloca(value.Type.GetLLVMType(_generator));
                _emitter.Builder.BuildStore(value.LLVMValue, tmp);
                value.LLVMValue = tmp;
            }

            var check = MugValue.From(
                _emitter.Builder.BuildICmp(
                    LLVMIntPredicate.LLVMIntEQ, _emitter.Builder.BuildLoad(GepOF(value.LLVMValue, 0)),
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, (uint)index)),
                MugValueType.Bool);

            if (alias.HasValue)
            {
                _emitter.Load(UnboxValue(value, boxtype, righttype));

                if (!_emitter.DeclareConstant(alias.Value.Value, alias.Value.Position))
                    return false;
            }    

            _emitter.Load(check);
            return true;
        }

        private MugValue UnboxValue(MugValue value, VariantStatement boxtype, MugValueType castType)
        {
            var ptr = castType.GetLLVMType(_generator) == _generator.GetBiggestTypeOFVariant(boxtype).GetLLVMType(_generator)
                ? value.LLVMValue : _emitter.EmitBitcast(value, castType).LLVMValue;

            ptr = GepOF(ptr, 1);

            return
                MugValue.From(_emitter.Builder.BuildLoad(ptr), castType);
        }

        private bool EmitExprArrayElemSelect(ArraySelectElemNode a, bool buildload = true)
        {
            // loading the array
            if (!EvaluateExpression(a.Left))
                return false;

            var indexed = _emitter.Pop();

            // loading the index expression
            if (!EvaluateExpression(a.IndexExpression))
                return false;

            // arrays are indexed by int32
            _emitter.CoerceConstantSizeTo(MugValueType.Int32);

            _emitter.ExpectIndexerType(a.IndexExpression.Position);

            var index = _emitter.Pop();

            if (indexed.Type.IsIndexable()) // loading the element
                _emitter.SelectArrayElement(buildload, GepOF(indexed.LLVMValue, 1), indexed.Type.ArrayBaseElementType, index);
            else
            {
                var parameters = new[] { indexed, index };
                _emitter.CallOperator(OperatorFunctionSymbol("[]", a.Position, ref parameters), a.Position, true, parameters);
            }

            return true;
        }

        private LLVMValueRef CreateHeapArray(MugValueType type, LLVMValueRef size, bool init = true, LLVMBuilderRef builder = default)
        {
            SetToEmitterWhenDefault(ref builder);

            var allocation = _emitter.Builder.BuildAlloca(type.GetLLVMType(_generator));
            var malloc = _emitter.Builder.BuildMalloc(type.GetLLVMType(_generator).ElementType);
            _emitter.Builder.BuildStore(malloc, allocation);
            _emitter.Builder.BuildStore(size, GepOF(_emitter.Builder.BuildLoad(allocation), 0));

            if (init)
            {
                var arr = _emitter.Builder.BuildArrayAlloca(type.ArrayBaseElementType.GetLLVMType(_generator), size);

                _emitter.Builder.BuildStore(arr, GepOF(_emitter.Builder.BuildLoad(allocation), 1));
            }

            return _emitter.Builder.BuildLoad(allocation);
        }

        private void StoreElementArray(LLVMValueRef arrayload, int i)
        {
            var ptr = _emitter.Builder.BuildGEP(_emitter.Builder.BuildLoad(GepOF(arrayload, 1)), new[]
            {
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)i)
            });

            _emitter.Builder.BuildStore(_emitter.Pop().LLVMValue, ptr);
        }

        private bool EmitExprAllocateArray(ArrayAllocationNode aa)
        {
            var arraytype = MugValueType.Array(aa.Type.ToMugValueType(_generator));

            // loading the array

            if (aa.SizeIsImplicit)
                _emitter.Load(MugValue.From(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (uint)aa.Body.Count), MugValueType.Int32));
            else if (!EvaluateExpression(aa.Size)) // make runtime check for out of bounds element writing
                return false;

            _emitter.CastInt(MugValueType.Int64);
            var allocation = CreateHeapArray(arraytype, _emitter.Pop().LLVMValue);

            var i = 0;

            foreach (var elem in aa.Body)
            {
                if (!EvaluateExpression(elem))
                    continue;

                _emitter.CoerceConstantSizeTo(arraytype.ArrayBaseElementType);

                if (!_emitter.PeekType().Equals(arraytype.ArrayBaseElementType))
                    Report(elem.Position, IRGenerator.ExpectTypeMessage(arraytype.ArrayBaseElementType, _emitter.PeekType()));

                StoreElementArray(allocation, i);

                i++;
            }

            _emitter.Load(MugValue.From(allocation, arraytype));
            return true;
        }

        private bool EmitExprAllocateStruct(TypeAllocationNode ta)
        {
            var structure = ta.Name.ToMugValueType(_generator);

            if (!structure.IsAllocableTypeNew())
                return Report(ta.Position, $"Unable to allocate type {ta.Name} with 'new' operator");

            var tmp = _emitter.Builder.BuildAlloca(structure.GetLLVMType(_generator));

            if (structure.IsEnum())
                return Report(ta.Position, "Unable to allocate an enum");

            var structureInfo = structure.GetStructure();

            var fields = new List<string>();

            for (int i = 0; i < ta.Body.Count; i++)
            {
                var field = ta.Body[i];

                if (fields.Contains(field.Name))
                    Report(field.Position, "Field reassignment in type allocation");

                fields.Add(field.Name);

                if (!EvaluateExpression(field.Body))
                    return false;

                if (!structureInfo.ContainsFieldWithName(field.Name))
                {
                    Report(field.Position, "Undeclared field");
                    continue;
                }

                var fieldType = structureInfo.GetFieldTypeFromName(field.Name, _generator, field.Position);

                _emitter.CoerceConstantSizeTo(fieldType);

                _generator.ExpectSameTypes(
                    fieldType, field.Body.Position, IRGenerator.ExpectTypeMessage(fieldType, _emitter.PeekType()), _emitter.PeekType());

                _emitter.StoreField(tmp, structureInfo.GetFieldIndexFromName(field.Name, _generator, field.Position));
            }

            for (int i = 0; i < structureInfo.FieldNames.Length; i++)
            {
                if (fields.Contains(structureInfo.FieldNames[i]))
                    continue;

                _emitter.Load(GetDefaultValueOf(structureInfo.FieldTypes[i], ta.Name.Position));

                _emitter.StoreField(tmp, i);
            }

            _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(tmp), structure));
            return true;
        }

        private bool EvaluateArrayFieldAccess(MugValue arr, string fieldname, bool buildload, ModulePosition memberposition)
        {
            /*if (!str.IsAllocaInstruction())
            {
                _emitter.Load(str);
                _emitter.MakeTempAllocation();
                str = _emitter.Pop();
            }*/

            // long len
            if (fieldname == "len64")
                loadLength();
            else if (fieldname == "len")
            {
                loadLength();
                _emitter.CastInt(MugValueType.Int32);
            }
            else
                return Report(memberposition, $"Undeclared field '{fieldname}'");

            return buildload || Report(memberposition, $"Field '{fieldname}' is readonly");

            void loadLength() => _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(GepOF(_emitter.Builder.BuildLoad(arr.LLVMValue), 0), "^len"), MugValueType.Int64));
        }

        private bool EmitExprMemberAccess(MemberNode m, bool buildload = true)
        {
            if (m.Base is Token token && token.Kind == TokenKind.Identifier)
            {
                if (_emitter.IsDeclared(token.Value))
                {
                    if (!_emitter.LoadFieldName(token.Value, token.Position))
                        return false;
                }
                else
                    return _emitter.LoadEnumMember(token.Value, m.Member.Value, m.Base.Position, m.Member.Position, this);
            }
            else
            {
                if (!EvaluateExpression(m.Base))
                    return false;

                _emitter.LoadFieldName();
            }

            var valuetype = _emitter.PeekType();

            if (!valuetype.IsStructure())
                return
                    valuetype.IsIndexable() ?
                    EvaluateArrayFieldAccess(_emitter.Pop(), m.Member.Value, buildload, m.Member.Position) :
                    Report(m.Base.Position, $"Type '{valuetype}' is not accessible via '.'");

            var structure = valuetype.GetStructure();
            var type = structure.GetFieldTypeFromName(m.Member.Value, _generator, m.Member.Position);
            var index = structure.GetFieldIndexFromName(m.Member.Value, _generator, m.Member.Position);
            var instance = _emitter.Pop();

            _emitter.LoadField(instance, type, index, buildload);
            return true;
        }

        private bool EvaluateTernary(ConditionalStatement cs)
        {
            var oldBuffer = _buffer;
            _buffer = MugValue.From(_emitter.Builder.BuildAlloca(LLVMTypeRef.Int32, ""), MugValueType.Undefinied);

            EmitConditionalStatement(cs);

            if (_buffer.Value.Type.TypeKind == MugValueTypeKind.Undefined)
                return Report(cs.Position, "Expected a non-void expression");

            _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(_buffer.Value.LLVMValue), _buffer.Value.Type));
            _buffer = oldBuffer;
            return true;
        }

        /// <summary>
        /// the function evaluates an expression, looking at the given node type
        /// </summary>
        private bool EvaluateExpression(INode expression)
        {
            switch (expression)
            {
                case ExpressionNode e: // binary expression: left op right
                    return EmitExpr(e);
                case Token t:
                    if (t.Kind == TokenKind.Identifier)
                        return _emitter.LoadFromMemory(t.Value, t.Position);
                    else // constant value
                        _emitter.Load(ConstToMugConst(t, t.Position));
                    break;
                case PrefixOperator p:
                    return EmitExprPrefixOperator(p);
                case PostfixOperator pp:
                    var left = EvaluateLeftValue(pp.Expression);
                    var load = _emitter.Builder.BuildLoad(left.LLVMValue);

                    EmitPostfixOperator(left, pp.Postfix, pp.Position, false);

                    _emitter.Load(MugValue.From(load, left.Type));
                    break;
                case CallStatement c:
                    // call statement inside expression, true as second parameter because an expression cannot be void
                    return EmitCallStatement(c, true);
                case CastExpressionNode ce:
                    // 'as' operator
                    if (!EvaluateExpression(ce.Expression))
                        return false;

                    return EmitCastInstruction(ce.Type, ce.Position);
                case BooleanExpressionNode b:
                    return EmitExprBool(b);
                case ArraySelectElemNode a:
                    return EmitExprArrayElemSelect(a);
                case ArrayAllocationNode aa:
                    EmitExprAllocateArray(aa);
                    break;
                case TypeAllocationNode ta:
                    return EmitExprAllocateStruct(ta);
                case MemberNode m:
                    return EmitExprMemberAccess(m);
                case CatchExpressionNode ce:
                    return EmitCatchStatement(ce, false);
                case AssignmentStatement ae:
                    EmitAssignmentStatement(ae);
                    return EvaluateExpression(ae.Name);
                case ConditionalStatement cs:
                    return MugParser.HasElseBody(cs) switch
                    {
                        false => Report(expression.Position, "Condition in expression must be exhaustive: missing else body"),
                        true => EvaluateTernary(cs)
                    };
                default:
                    Error(expression.Position, "Expression not supported yet");
                    break;
            }

            return true;
        }

        /// <summary>
        /// functions that do not return a value must still have a ret instruction in the low level representation,
        /// this function manages the implicit emission of the ret instruction when it is not done by the user.
        /// see the caller to better understand
        /// </summary>
        public void AddImplicitRetVoid()
        {
            _emitter.RetVoid();
        }

        private void AllocParameters()
        {
            if (_function.Base.HasValue)
            {
                var baseparameter = _function.Base.Value;
                var type = baseparameter.Type.ToMugValueType(_generator);

                _emitter.Load(MugValue.From(_llvmfunction.GetParam(0), type, true));
                _emitter.DeclareConstant(baseparameter.Name, baseparameter.Position);
            }

            var offset = _function.Base.HasValue ? (uint)1 : 0;

            // alias for ...
            var parameters = _function.ParameterList.Parameters;

            for (int i = 0; i < parameters.Count; i++)
            {
                // alias for ...
                var parameter = parameters[i];

                var parametertype = parameter.Type.ToMugValueType(_generator);

                // allocating the local variable
                _emitter.DeclareVariable(
                    parameter.Name,
                    parametertype,
                    parameter.Position);

                // storing the parameter into the variable
                _emitter.InitializeParameter(parameter.Name, _llvmfunction.GetParam((uint)i + offset));
            }
        }

        private void DefineElseBody(LLVMBasicBlockRef @else, LLVMBasicBlockRef endifelse, ConditionalStatement statement, MugEmitter oldemitter)
        {
            // is elif
            if (statement.Kind == TokenKind.KeyElif)
            {
                // preparing the else if body
                _emitter = new MugEmitter(_generator, oldemitter.Memory, endifelse, true);
                _emitter.Builder.PositionAtEnd(@else);

                // evaluating the else if expression
                if (!EvaluateConditionExpression(statement.Expression, statement.Position))
                    return;

                // creating a new block, the current will be used to decide if jump to the else if body or the next condition/end
                var elseif = _llvmfunction.AppendBasicBlock("");

                // the next condition
                var next = statement.ElseNode is not null ? _llvmfunction.AppendBasicBlock("") : endifelse;
                
                // branch the current or the next
                _emitter.CompareJump(elseif, next);
                // locating the new block
                _emitter.Builder.PositionAtEnd(elseif);

                // generating the low-level code
                Generate(statement.Body);
                // back to the main block, jump ou of the if scope
                _emitter.JumpOutOfScope(elseif.Terminator, endifelse);

                // check if there is another else node
                if (statement.ElseNode is not null)
                    DefineElseBody(next, endifelse, statement.ElseNode, oldemitter);
            }
            else // is else
                DefineConditionBody(@else, endifelse, statement.Body, oldemitter);
        }

        private void DefineConditionBody(
            LLVMBasicBlockRef then,
            LLVMBasicBlockRef endifelse,
            BlockNode body,
            MugEmitter oldemitter
            /*bool isCycle = false, LLVMBasicBlockRef cycleExitBlock = new()*/)
        {
            // allocating a new emitter with the old symbols
            _emitter = new MugEmitter(_generator, oldemitter.Memory, endifelse, true);
            // locating the emitter builder at the end of the block
            _emitter.Builder.PositionAtEnd(then);

            /*if (isCycle)
                _emitter.CycleExitBlock = cycleExitBlock;*/
            
            // generating the low-level code
            Generate(body);
            
            // back to the main block, jump out of the if scope
            _emitter.JumpOutOfScope(then.Terminator, endifelse);
        }

        private bool EvaluateConditionExpression(INode expression, ModulePosition position, bool allowNull = false)
        {
            if (allowNull && expression is null)
            {
                _emitter.Load(MugValue.From(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1), MugValueType.Bool));
                return true;
            }

            // evaluating conditional expression
            if (!EvaluateExpression(expression))
                return false;
            // make sure the expression returned bool
            _generator.ExpectBoolType(_emitter.PeekType(), position);
            return true;
        }

        private void EmitIfStatement(ConditionalStatement i)
        {
            // evaluate expression
            if (!EvaluateConditionExpression(i.Expression, i.Expression.Position))
                return;

            var saveOldCondition = _oldcondition;

            // if block
            var then = _llvmfunction.AppendBasicBlock("");

            _oldcondition = then;

            // else block
            var @else = i.ElseNode is not null ? _llvmfunction.AppendBasicBlock("") : _emitter.ExitBlock;

            var endcondition = _llvmfunction.AppendBasicBlock("");

            // compare
            _emitter.CompareJump(then, i.ElseNode is null ? endcondition : @else);

            // save the old emitter
            var oldemitter = _emitter;

            // define if and else bodies
            // if body
            DefineConditionBody(then, endcondition, i.Body, oldemitter);

            // else body
            if (i.ElseNode is not null)
                DefineElseBody(@else, endcondition, i.ElseNode, oldemitter);

            // restore old emitter
            _emitter = new(_generator, oldemitter.Memory, oldemitter.ExitBlock, oldemitter.IsInsideSubBlock);

            /*if (_emitter.IsInsideSubBlock)
            {
                if (i.ElseNode is not null)
                    saveOldCondition.Terminator.SetOperand(1, @else.AsValue());
                else
                    saveOldCondition.Terminator.SetOperand(1, endcondition.AsValue());
            }*/

            _oldcondition = saveOldCondition;

            // re emit the entry block
            _emitter.Builder.PositionAtEnd(endcondition);
        }

        private void EmitWhileStatement(ConditionalStatement i)
        {
            // if block
            var compare = _llvmfunction.AppendBasicBlock("");

            var cycle = _llvmfunction.AppendBasicBlock("");

            var endcycle = _llvmfunction.AppendBasicBlock("");

            var saveOldCondition = _oldcondition;

            _oldcondition = cycle; // compare here

            // jumping to the compare block
            _emitter.Jump(compare);

            // save the old emitter
            var oldemitter = _emitter;

            _emitter = new(_generator, oldemitter.Memory, cycle, true);
            // locating the builder in the compare block
            _emitter.Builder.PositionAtEnd(compare);

            // evaluate expression
            if (!EvaluateConditionExpression(i.Expression, i.Position))
                return;

            // compare
            _emitter.CompareJump(cycle, endcycle);

            var oldCycleExitBlock = CycleExitBlock;
            var oldCycleCompareBlock = CycleCompareBlock;

            CycleExitBlock = endcycle;
            CycleCompareBlock = compare;

            // define if and else bodies
            DefineConditionBody(cycle, compare, i.Body, oldemitter);

            // restore old emitter
            _emitter = new(_generator, oldemitter.Memory, oldemitter.ExitBlock, oldemitter.IsInsideSubBlock);

            /*if (_emitter.IsInsideSubBlock)
            {
                if (saveOldCondition.Terminator.OperandCount >= 2)
                    saveOldCondition.Terminator.SetOperand(1, endcycle.AsValue());
            }*/

            // re emit the entry block
            _emitter.Builder.PositionAtEnd(endcycle);

            CycleExitBlock = oldCycleExitBlock;
            CycleCompareBlock = oldCycleCompareBlock;
            _oldcondition = saveOldCondition;
        }

        private void EmitConditionalStatement(ConditionalStatement i)
        {
            if (i.Kind == TokenKind.KeyIf)
                EmitIfStatement(i);
            else
                EmitWhileStatement(i);
        }

        private MugValue GetDefaultValueOfDefinedType(MugValueType type, ModulePosition position)
        {
            if (type.IsEnum())
            {
                var enumerated = type.GetEnum();
                var first = ConstToMugConst(enumerated.Body.First().Value, position, true, enumerated.BaseType.ToMugValueType(_generator));
                return MugValue.EnumMember(first.Type, first.LLVMValue);
            }

            var structure = type.GetStructure();

            var tmp = _emitter.Builder.BuildAlloca(structure.LLVMValue);

            for (int i = 0; i < structure.FieldNames.Length; i++)
            {
                _emitter.Load(GetDefaultValueOf(structure.FieldTypes[i], structure.FieldPositions[i]));

                _emitter.StoreField(tmp, i);
            }

            return MugValue.From(_emitter.Builder.BuildLoad(tmp), type);
        }

        private MugValue ReferencesPointersMandatoryInitialization(ModulePosition position, MugValueType type)
        {
            _generator.Report(position, "References and pointers must be initialized");
            return MugValue.From(LLVMValueRef.CreateConstAllOnes(type.GetLLVMType(_generator)), type);
        }

        private MugValue ReportUnitializedVariant(ModulePosition position, MugValueType type)
        {
            _generator.Error(position, "Variants must be initilized");
            return MugValue.From(LLVMValueRef.CreateConstAllOnes(type.GetLLVMType(_generator)), type);
        }

        private MugValue DefaultArrayValue(MugValueType type, ModulePosition position)
        {
            var size = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0);
            var emptyarray = CreateHeapArray(type, size, false); // to change

            return MugValue.From(emptyarray, type, isconstant: true);
        }

        private MugValue GetDefaultValueOf(MugValueType type, ModulePosition position)
        {
            return type.TypeKind switch
            {
                MugValueTypeKind.Char or
                MugValueTypeKind.Int8 or
                MugValueTypeKind.Int32 or
                MugValueTypeKind.Int64 or
                MugValueTypeKind.Bool => MugValue.From(LLVMValueRef.CreateConstInt(type.GetLLVMType(_generator), 0), type, true),
                MugValueTypeKind.Float32 or
                MugValueTypeKind.Float64 or
                MugValueTypeKind.Float128 => MugValue.From(LLVMValueRef.CreateConstSIToFP(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0), type.GetLLVMType(_generator)), type, true),
                MugValueTypeKind.String => MugValue.From(CreateConstString("", false), type, true),
                MugValueTypeKind.Array => DefaultArrayValue(type, position),
                MugValueTypeKind.Enum or
                MugValueTypeKind.Struct => GetDefaultValueOfDefinedType(type, position),
                MugValueTypeKind.Variant => ReportUnitializedVariant(position, type),
                MugValueTypeKind.Unknown or
                MugValueTypeKind.Reference or
                MugValueTypeKind.Pointer => ReferencesPointersMandatoryInitialization(position, type),
            };
        }

        private readonly LLVMValueRef Negative1 = LLVMValueRef.CreateConstNeg(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, 1));

        private void EmitReturnStatement(ReturnStatement @return)
        {
            var type = _function.ReturnType.ToMugValueType(_generator);
            /*
             * if the expression in the return statement is null, condition verified by calling Returnstatement.Isvoid(),
             * check that the type of function in which it is found returns void.
             */
            if (@return.IsVoid())
            {
                if (type.IsEnumError() && type.GetEnumError().SuccessType.TypeKind == MugValueTypeKind.Void)
                {
                    _emitter.Load(MugValue.From(Negative1, type));
                    _emitter.Ret();
                }
                else if (type.TypeKind == MugValueTypeKind.Void)
                    _emitter.RetVoid();
                else
                {
                    Report(@return.Position, "Expected non-void expression");
                    return;
                }
            }
            else
            {
                /*
                 * if instead the expression of the return statement has is nothing,
                 * it will be evaluated and then it will be compared the type of the result with the type of return of the function
                 */
                if (!EvaluateExpression(@return.Body))
                    return;

                _emitter.CoerceConstantSizeTo(type);
                
                var exprType = _emitter.PeekType();
                var errorMessage = IRGenerator.ExpectTypeMessage(type, exprType);

                if (type.IsEnumError())
                {
                    var enumerrorType = type.GetEnumError();
                    LLVMValueRef? value = null;
                    LLVMValueRef error;

                    if (exprType.Equals(enumerrorType.ErrorType))
                        error = _emitter.Pop().LLVMValue;
                    else
                    {
                        _emitter.CoerceConstantSizeTo(enumerrorType.SuccessType);
                        exprType = _emitter.PeekType();

                        if (exprType.Equals(enumerrorType.SuccessType))
                        {
                            value = enumerrorType.SuccessType.TypeKind == MugValueTypeKind.Void ? new() : _emitter.Pop().LLVMValue;
                            error = Negative1;
                        }
                        else
                        {
                            Report(@return.Position, errorMessage);
                            return;
                        }
                    }

                    if (enumerrorType.SuccessType.TypeKind != MugValueTypeKind.Void) {
                        var tmp = _emitter.Builder.BuildAlloca(type.GetLLVMType(_generator));

                        if (value.HasValue)
                            _emitter.Builder.BuildStore(
                                value.Value,
                                _emitter.Builder.BuildGEP(tmp, new[]
                                {
                                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1)
                                }));

                        _emitter.Builder.BuildStore(
                            error,
                            _emitter.Builder.BuildGEP(tmp, new[]
                            {
                                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0)
                            }));

                        _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(tmp), type));
                    }
                    else
                        _emitter.Load(MugValue.From(error, type));

                    _emitter.Ret();
                }
                else
                {
                    _generator.ExpectSameTypes(type, @return.Position, errorMessage, exprType);

                    _emitter.Ret();
                }
            }
        }

        private void EmitVariableStatement(VariableStatement variable)
        {
            _generator.ExpectNonVoidType(variable.Type, variable.Position);

            if (!variable.IsAssigned)
            {
                if (variable.Type.IsAutomatic())
                {
                    Report(variable.Position, "Type specification needed");
                    return;
                }

                _emitter.Load(GetDefaultValueOf(variable.Type.ToMugValueType(_generator), variable.Position));
            }
            else if (!EvaluateExpression(variable.Body)) // the expression in the variable’s body is evaluated
                return;

            var type = !variable.Type.IsAutomatic() ? variable.Type.ToMugValueType(_generator) : _emitter.PeekType();

            /*
             * if in the statement of variable the type is specified explicitly,
             * then a check will be made: the specified type and the type of the result of the expression must be the same.
             */
            // if the type is not specified, it will come directly allocate a variable with the same type as the expression result
            _emitter.DeclareVariable(
                variable.Name,
                type,
                variable.Position);

            _emitter.StoreVariable(variable.Name, variable.Position, variable.Body is not null ? variable.Body.Position : variable.Position);
        }

        private string PostfixOperatorToString(TokenKind kind)
        {
            return kind switch
            {
                TokenKind.OperatorIncrement => "++",
                TokenKind.OperatorDecrement => "--",
                _ => throw new Exception("unreachable")
            };
        }

        private void EmitPostfixOperator(MugValue variabile, TokenKind kind, ModulePosition position, bool isStatement)
        {
            _emitter.Load(variabile);

            if (_emitter.PeekType().MatchIntType())
            {
                if (kind == TokenKind.OperatorIncrement)
                    _emitter.MakePostfixIntOperation(_emitter.Builder.BuildAdd);
                else
                    _emitter.MakePostfixIntOperation(_emitter.Builder.BuildSub);
            }
            else
            {
                var parameters = new[] { _emitter.Pop() };
                _emitter.CallOperator(OperatorFunctionSymbol(PostfixOperatorToString(kind), position, ref parameters), position, !isStatement, parameters);
            }
        }

        /// <summary>
        /// returns a llvm pointer to store the expression in
        /// </summary>
        private MugValue EvaluateLeftValue(INode leftexpression, bool isfirst = true)
        {
            MugValue result;
            if (leftexpression is Token token && token.Kind == TokenKind.Identifier)
            {
                var allocation = _emitter.GetMemoryAllocation(token.Value, token.Position);
                if (!allocation.HasValue)
                    Stop();

                result = allocation.Value;
            }
            else if (leftexpression is ArraySelectElemNode indexing)
            {
                if (!EmitExprArrayElemSelect(indexing, false))
                    Stop();

                result = _emitter.Pop();
            }
            else if (leftexpression is MemberNode member)
            {
                if (!EmitExprMemberAccess(member, false))
                    Stop();

                result = _emitter.Pop();
            }
            else if (leftexpression is PrefixOperator prefix)
            {
                if (prefix.Prefix != TokenKind.Star)
                    Error(leftexpression.Position, "Unable to assign a value to an expression");

                var ptr = EvaluateLeftValue(prefix.Expression, false);
                
                if (!ptr.Type.IsPointer(false))
                    Error(leftexpression.Position, "Cannot apply operator '*' to a left value which is not a pointer in assigment");

               result = MugValue.From(ptr.IsConst ? ptr.LLVMValue : _emitter.LoadFromPointer(ptr, prefix.Position).LLVMValue, ptr.Type.PointerBaseElementType);
            }
            else
            {
                Report(leftexpression.Position, "Illegal left expression");
                Stop();
                throw new();
            }

            return result;
        }

        private void EmitAssignmentStatement(AssignmentStatement assignment)
        {
            var ptr = EvaluateLeftValue(assignment.Name);

            if (ptr.IsConst)
            {
                Report(assignment.Position, "Unable to change a constant value");
                return;
            }

            if (!EvaluateExpression(assignment.Body))
                return;

            _emitter.CoerceConstantSizeTo(ptr.Type);

            if (assignment.Operator == TokenKind.Equal)
            {
                _generator.ExpectSameTypes(_emitter.PeekType(), assignment.Position, IRGenerator.ExpectTypeMessage(ptr.Type, _emitter.PeekType()), ptr.Type);
                _emitter.StoreInsidePointer(ptr);
            }
            else
            {
                _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(ptr.LLVMValue), ptr.Type));
                _emitter.Swap();

                switch (assignment.Operator)
                {
                    case TokenKind.AddAssignment: EmitSum(assignment.Position); break;
                    case TokenKind.SubAssignment: EmitSub(assignment.Position); break;
                    case TokenKind.MulAssignment: EmitMul(assignment.Position); break;
                    case TokenKind.DivAssignment: EmitDiv(assignment.Position); break;
                    default: throw new();
                }

                _emitter.OperateInsidePointer(ptr);
            }
        }

        private void EmitConstantStatement(ConstantStatement constant)
        {
            // evaluating the body expression of the constant
            if (!EvaluateExpression(constant.Body))
                return;

            // match the constant explicit type and expression type are the same
            if (!constant.Type.IsAutomatic())
            {
                var constType = constant.Type.ToMugValueType(_generator);

                _emitter.CoerceConstantSizeTo(constType);

                _generator.ExpectSameTypes(constType,
                    constant.Body.Position, IRGenerator.ExpectTypeMessage(constType, _emitter.PeekType()), _emitter.PeekType());
            }

            // declaring the constant with a name
            _emitter.DeclareConstant(constant.Name, constant.Position);
        }

        private void EmitLoopManagementStatement(LoopManagementStatement management)
        {
            // is not inside a cycle
            if (CycleExitBlock.Handle == IntPtr.Zero)
                Report(management.Position, "'break' only allowed inside cycles' and catches' block");
            else if (management.Management.Kind == TokenKind.KeyBreak)
                _emitter.Jump(CycleExitBlock);
            else if (CycleCompareBlock.Handle == IntPtr.Zero)
                Report(management.Position, "'continue' only allowed inside cycles' block");
            else
                _emitter.Jump(CycleCompareBlock);
        }

        private MugValue LoadField(ref LLVMValueRef tmp, EnumErrorInfo enumerror, uint index)
        {
            var gep = _emitter.Builder.BuildGEP(tmp, new[]
            {
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0),
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, index)
            });

            return MugValue.From(_emitter.Builder.BuildLoad(gep), enumerror.ErrorType);
        }

        private MugValue? _buffer = null;

        private bool EmitCatchStatement(CatchExpressionNode catchstatement, bool isImperativeStatement)
        {
            if (catchstatement.Expression is not CallStatement call)
                return Report(catchstatement.Expression.Position, "Unable to catch this expression");

            if (!EmitCallStatement(call, true, true))
                return false;

            var value = _emitter.Pop();
            var enumerror = value.Type.GetEnumError();
            var tmp = _emitter.Builder.BuildAlloca(enumerror.LLVMValue);
            var resultIsVoid = enumerror.SuccessType.TypeKind == MugValueTypeKind.Void;
            var oldBuffer = _buffer;
            var oldCycleExitBlock = CycleExitBlock;

            _emitter.Builder.BuildStore(value.LLVMValue, tmp);
            _buffer = resultIsVoid ? MugValue.From(new(), MugValueType.Void) : MugValue.From(_emitter.Builder.BuildAlloca(enumerror.SuccessType.GetLLVMType(_generator)), enumerror.SuccessType);

            if (resultIsVoid)
                _emitter.Load(MugValue.From(value.LLVMValue, enumerror.ErrorType));
            else
            {
                // _emitter.Builder.BuildStore(GetDefaultValueOf(enumerror.SuccessType, call.Position).LLVMValue, _buffer.Value.LLVMValue);
                _emitter.Load(LoadField(ref tmp, enumerror, 0));
            }

            var catchbodyErr = _llvmfunction.AppendBasicBlock("");
            var catchbodyOk = resultIsVoid || isImperativeStatement ? new() : _llvmfunction.AppendBasicBlock("");
            var catchend = _llvmfunction.AppendBasicBlock("");

            _emitter.Builder.BuildCondBr(
                _emitter.Builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, _emitter.Pop().LLVMValue, Negative1),
                catchbodyErr,
                resultIsVoid || isImperativeStatement ? catchend : catchbodyOk);

            var oldemitter = _emitter;
            var oldMemory = _emitter.Memory;

            if (!resultIsVoid && !isImperativeStatement)
            {
                _emitter = new MugEmitter(_generator, _emitter.Memory, catchend, true);
                _emitter.Builder.PositionAtEnd(catchbodyOk);

                _emitter.Builder.BuildStore(LoadField(ref tmp, enumerror, 1).LLVMValue, _buffer.Value.LLVMValue);
                _emitter.Exit();

                _emitter = new MugEmitter(_generator, _emitter.Memory, catchend, oldemitter.IsInsideSubBlock);
            }

            oldemitter = _emitter;
            _emitter = new MugEmitter(_generator, _emitter.Memory, catchend, true);
            _emitter.Builder.PositionAtEnd(catchbodyErr);

            CycleExitBlock = catchend;

            if (catchstatement.OutError is not null)
            {
                _emitter.Load(resultIsVoid ? value : LoadField(ref tmp, enumerror, 0));
                _emitter.DeclareConstant(catchstatement.OutError.Value.Value, catchstatement.OutError.Value.Position);
            }

            Generate(catchstatement.Body);

            _emitter.Exit();

            _emitter = new MugEmitter(_generator, oldMemory, catchend, oldemitter.IsInsideSubBlock);
            _emitter.Builder.PositionAtEnd(catchend);

            if (!isImperativeStatement)
            {
                if (resultIsVoid)
                    return Report(catchstatement.Expression.Position, "Unable to evaluate void in expression");

                _emitter.Load(MugValue.From(_emitter.Builder.BuildLoad(_buffer.Value.LLVMValue), _buffer.Value.Type));
            }

            _buffer = oldBuffer;
            CycleExitBlock = oldCycleExitBlock;

            return true;
        }

        private void EmitForStatement(ForLoopStatement forstatement)
        {
            // if block
            var compare = _llvmfunction.AppendBasicBlock("");

            var cycle = _llvmfunction.AppendBasicBlock("");

            var operate = _llvmfunction.AppendBasicBlock("");

            var endcycle = _llvmfunction.AppendBasicBlock("");

            var saveOldCondition = _oldcondition;

            _oldcondition = cycle; // compare here

            var oldMemory = _emitter.Memory;

            if (forstatement.LeftExpression is not null)
            {
                _emitter.ReallocMemory();
                EmitVariableStatement(forstatement.LeftExpression);
            }

            // jumping to the compare block
            _emitter.Jump(compare);

            // save the old emitter
            var oldemitter = _emitter;

            _emitter = new(_generator, oldemitter.Memory, cycle, true);
            // locating the builder in the compare block
            _emitter.Builder.PositionAtEnd(operate);

            if (forstatement.RightExpression is not null)
                RecognizeStatement(forstatement.RightExpression, false);

            _emitter.Jump(compare);

            // locating the builder in the compare block
            _emitter.Builder.PositionAtEnd(compare);

            // evaluate expression
            if (!EvaluateConditionExpression(forstatement.ConditionExpression, forstatement.Position, true))
                return;

            // compare
            _emitter.CompareJump(cycle, endcycle);

            var oldCycleExitBlock = CycleExitBlock;
            var oldCycleCompareBlock = CycleCompareBlock;

            CycleExitBlock = endcycle;
            CycleCompareBlock = operate;

            // define if and else bodies
            DefineConditionBody(cycle, operate, forstatement.Body, oldemitter);

            // restore old emitter
            _emitter = new(_generator, oldMemory, oldemitter.ExitBlock, oldemitter.IsInsideSubBlock);

            // re emit the entry block
            _emitter.Builder.PositionAtEnd(endcycle);

            CycleExitBlock = oldCycleExitBlock;
            CycleCompareBlock = oldCycleCompareBlock;
            _oldcondition = saveOldCondition;
        }

        private void StoreInHiddenBuffer(ModulePosition position, bool isLastOFBlock)
        {
            if (!_buffer.HasValue || !isLastOFBlock)
                return;

            if (_buffer.Value.Type.TypeKind == MugValueTypeKind.Undefined)
            {
                var oldemitterBuilder = _emitter.Builder;
                var tmp = _buffer.Value;
                tmp.Type = _emitter.PeekType();
                unsafe { _emitter.Builder = LLVM.CreateBuilder(); }
                _emitter.Builder.PositionBefore(tmp.LLVMValue);
                var x = _emitter.Builder.BuildAlloca(tmp.Type.GetLLVMType(_generator));
                tmp.LLVMValue.InstructionEraseFromParent();
                tmp.LLVMValue = x;
                _emitter.Builder = oldemitterBuilder;
                _buffer = tmp;
            }
            else if (!_buffer.Value.Type.Equals(_emitter.PeekType()))
                Report(position, IRGenerator.ExpectTypeMessage(_buffer.Value.Type, _emitter.PeekType()));

            _emitter.Builder.BuildStore(_emitter.Pop().LLVMValue, _buffer.Value.LLVMValue);
        }

        private void RecognizeStatement(INode statement, bool isLastOFBlock)
        {
            switch (statement)
            {
                case VariableStatement variable:
                    EmitVariableStatement(variable);
                    break;
                case ReturnStatement @return:
                    EmitReturnStatement(@return);
                    break;
                case ConditionalStatement condition:
                    if (_buffer.HasValue && isLastOFBlock)
                        EvaluateTernary(condition);
                    else
                        EmitConditionalStatement(condition);

                    StoreInHiddenBuffer(statement.Position, isLastOFBlock);
                    break;
                case CallStatement call:
                    var stackcount = _emitter.StackCount;
                    EmitCallStatement(call, false);

                    if (_emitter.StackCount > stackcount) // returned a value
                        StoreInHiddenBuffer(statement.Position, isLastOFBlock);
                    break;
                case AssignmentStatement assignment:
                    EmitAssignmentStatement(assignment);
                    break;
                case ConstantStatement constant:
                    EmitConstantStatement(constant);
                    break;
                case LoopManagementStatement loopmanagement:
                    EmitLoopManagementStatement(loopmanagement);
                    break;
                case CatchExpressionNode catchstatement:
                    EmitCatchStatement(catchstatement, !_buffer.HasValue && !isLastOFBlock);

                    StoreInHiddenBuffer(statement.Position, isLastOFBlock);
                    break;
                case PostfixOperator postfix:
                    var left = EvaluateLeftValue(postfix.Expression);

                    EmitPostfixOperator(left, postfix.Postfix, postfix.Position, true);
                    break;
                case PrefixOperator prefix:
                    if (prefix.Prefix == TokenKind.OperatorIncrement || prefix.Prefix == TokenKind.OperatorDecrement)
                    {
                        left = EvaluateLeftValue(prefix.Expression);

                        EmitPostfixOperator(left, prefix.Prefix, prefix.Position, true);
                    }
                    else
                        goto default;
                    break;
                case ForLoopStatement forstatement:
                    EmitForStatement(forstatement);
                    break;
                default:
                    if (!EvaluateExpression(statement))
                        return;

                    if (!_buffer.HasValue)
                    {
                        Report(statement.Position, "Unable to evaluate expression in this scope");
                        return;
                    }

                    if (!isLastOFBlock)
                    {
                        Report(statement.Position, "Expressions evaluable only when last in block");
                        return;
                    }

                    StoreInHiddenBuffer(statement.Position, isLastOFBlock);
                    break;
            }
        }

        /// <summary>
        /// the function cycles all the nodes in the statement array passed
        /// </summary>
        public void Generate(BlockNode statements)
        {
            for (int i = 0; i < statements.Statements.Length; i++)
                RecognizeStatement(statements.Statements[i], i == statements.Statements.Length - 1);

            if (_emitter.IsInsideSubBlock)
                _emitter.Exit();
        }

        /// <summary>
        /// the function passes all the nodes in the statement array of
        /// a Functionnode to the <see cref="Generate(BlockNode)"/> function and
        /// calls a function to convert them into the corresponding low-level code
        /// </summary>
        public void Generate()
        {
            // allocating parameters as local variable
            AllocParameters();
            Generate(_function.Body);
        }
    }
}
