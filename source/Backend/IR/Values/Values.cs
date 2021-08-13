using Mug.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Backend.IR.Values
{
    public struct LoadFunctionInst : IRValue
    {
        public IRBlock Block { get; }

        public IRUnsolvedType[] ParameterTypes { get; }

        public IRUnsolvedType ReturnType { get; }

        public ModulePosition Position { get; }

        public LoadFunctionInst(IRBlock block, IRUnsolvedType[] parameterTypes, IRUnsolvedType returnType, ModulePosition position)
        {
            Block = block;
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
            Position = position;
        }

        public override string ToString()
        {
            return $"load_fn({string.Join(", ", ParameterTypes)}) -> {ReturnType} {Block}";
        }
    }

    public class IRBlock
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
        public ModulePosition Position { get; }

        public override string ToString() => "?";
    }

    public struct LoadNameInst: IRValue
    {
        public string Name { get; }

        public ModulePosition Position { get; }

        public LoadNameInst(string name, ModulePosition position)
        {
            Name = name;
            Position = position;
        }

        public override string ToString() => $"load_name '{Name}'";
    }

    public struct LoadIntegerInst : IRValue
    {
        public ulong Integer { get; }

        public ModulePosition Position { get; }

        public LoadIntegerInst(ulong value, ModulePosition position)
        {
            Integer = value;
            Position = position;
        }

        public override string ToString() => $"load_integer {Integer}";
    }

    public struct LoadDecimalInst : IRValue
    {
        public decimal Decimal { get; }

        public ModulePosition Position { get; }

        public LoadDecimalInst(decimal value, ModulePosition position)
        {
            Decimal = value;
            Position = position;
        }

        public override string ToString() => $"load_decimal {Decimal}";
    }

    public struct LoadBooleanInst : IRValue
    {
        public bool Boolean { get; }

        public ModulePosition Position { get; }

        public LoadBooleanInst(bool value, ModulePosition position)
        {
            Boolean = value;
            Position = position;
        }

        public override string ToString() => $"load_boolean {Boolean}";
    }

    public struct LoadCharInst : IRValue
    {
        public char Value { get; }

        public ModulePosition Position { get; }

        public LoadCharInst(char value, ModulePosition position)
        {
            Value = value;
            Position = position;
        }

        public override string ToString() => $"load_char '{Value}'";
    }

    public struct LoadStringInst : IRValue
    {
        public string Value { get; }

        public ModulePosition Position { get; }

        public LoadStringInst(string value, ModulePosition position)
        {
            Value = value;
            Position = position;
        }

        public override string ToString() => $"load_string '{Value}'";
    }

    public struct ReturnInst : IRValue
    {
        public ModulePosition Position { get; }

        public ReturnInst(ModulePosition position)
        {
            Position = position;
        }

        public override string ToString() => $"ret";
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

        public IRUnsolvedType Type { get; }

        public ModulePosition Position { get; }

        public DeclareVariableInst(VariableKind kind, string name, IRUnsolvedType type, ModulePosition position)
        {
            Kind = kind;
            Name = name;
            Type = type;
            Position = position;
        }

        public override string ToString() => $"declare_variable !{Kind} %{Name} -> {Type}";
    }

    public struct BinInst : IRValue
    {
        public enum OpKind
        {
            Add,
            Sub,
            Mul,
            Div
        }

        public OpKind Kind { get; }

        public ModulePosition Position { get; }

        public BinInst(OpKind kind, ModulePosition position)
        {
            Kind = kind;
            Position = position;
        }

        public override string ToString() => $"bin_op !{Kind}";
    }

    public struct CallInst : IRValue
    {
        public uint ParametersCount { get; }

        public bool Builtin { get; }

        public ModulePosition Position { get; }

        public CallInst(uint parametersCount, bool builtin, ModulePosition position)
        {
            ParametersCount = parametersCount;
            Builtin = builtin;
            Position = position;
        }

        public override string ToString() => $"call !{ParametersCount}{(Builtin ? ", !builtin" : null)}";
    }

    public struct DequeueParameterInst : IRValue
    {
        public ModulePosition Position { get; }

        public DequeueParameterInst(ModulePosition position)
        {
            Position = position;
        }

        public override string ToString() => $"dequeue_parameter";
    }

    public struct IRUnsolvedType : IRValue
    {
        public IRBlock Type { get; }

        public ModulePosition Position { get; }

        public IRUnsolvedType(IRBlock type, ModulePosition position)
        {
            Type = type;
            Position = position;
        }

        public override string ToString() => $"@({Type})";

        public static IRUnsolvedType Auto => new(null, default);
    }

    public struct LoadMemberInst : IRValue
    {
        public string Name { get; }

        public ModulePosition Position { get; }

        public LoadMemberInst(string name, ModulePosition position)
        {
            Name = name;
            Position = position;
        }

        public override string ToString() => $"load_member '{Name}'";
    }
}