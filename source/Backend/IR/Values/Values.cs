using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Backend.IR.Values
{
    public struct FunctionValue : IRValue
    {
        public IRBlock Block { get; }

        public IRValue[] ParameterTypes { get; }

        public IRValue ReturnType { get; }

        public FunctionValue(IRBlock block, IRValue[] parameterTypes, IRValue returnType)
        {
            Block = block;
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
        }

        public override string ToString()
        {
            return $"fn() -> {ReturnType} {Block}";
        }
    }

    public class IRBlock : IRValue
    {
        private static string _indentation = "";

        public List<IRValue> Values { get; } = new();

        public override string ToString()
        {
            _indentation += "  ";
            var result = new StringBuilder("{\n");

            foreach (var value in Values)
                result.AppendLine($"{_indentation}{value};");

            result.Append($"{_indentation = _indentation[..^2]}}}");
            return result.ToString();
        }
    }

    public struct BadIRValue : IRValue
    {
        public override string ToString() => "?";
    }

    public struct NameValue: IRValue
    {
        public string Name { get; }

        public NameValue(string name)
        {
            Name = name;
        }

        public override string ToString() => $"name '{Name}'";
    }

    public struct IntegerValue : IRValue
    {
        public ulong Integer { get; }

        public IntegerValue(ulong value)
        {
            Integer = value;
        }

        public override string ToString() => $"integer {Integer}";
    }

    public struct DecimalValue : IRValue
    {
        public decimal Decimal { get; }

        public DecimalValue(decimal value)
        {
            Decimal = value;
        }

        public override string ToString() => $"decimal {Decimal}";
    }

    public struct BooleanValue : IRValue
    {
        public bool Boolean { get; }

        public BooleanValue(bool value)
        {
            Boolean = value;
        }

        public override string ToString() => $"boolean {Boolean}";
    }

    public struct CharValue : IRValue
    {
        public char Value { get; }

        public CharValue(char value)
        {
            Value = value;
        }

        public override string ToString() => $"char '{Value}'";
    }

    public struct StringValue : IRValue
    {
        public string Value { get; }

        public StringValue(string value)
        {
            Value = value;
        }

        public override string ToString() => $"string '{Value}'";
    }

    public struct ReturnInst : IRValue
    {
        public IRValue Value { get; }

        public ReturnInst(IRValue value)
        {
            Value = value;
        }

        public override string ToString() => $"ret {Value}";
    }

    public struct DeclareVariableInst : IRValue
    {
        public enum VariableKind
        {
            Const,
            Let,
            LetMut
        }

        public VariableKind Kind { get; }

        public string Name { get; }

        public IRValue Type { get; }

        public IRValue Value { get; }

        public DeclareVariableInst(VariableKind kind, string name, IRValue type, IRValue value)
        {
            Kind = kind;
            Name = name;
            Type = type;
            Value = value;
        }

        public override string ToString() => $"declare_variable !{Kind} %{Name} -> {Type} = {Value}";
    }
}