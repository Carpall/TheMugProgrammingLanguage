using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Generator.TargetGenerators.C
{
    class CModuleBuilder
    {
        public string Name { get; }
        public List<CFunctionBuilder> Functions { get; } = new();
        public List<string> FunctionPrototypes { get; } = new();
        public List<CStructureBuilder> Structures { get; } = new();
        public List<(string Symbol, string Value)> Defines { get; } = new();
        public List<string> Globals { get; } = new();

        public CModuleBuilder(string name)
        {
            Name = name;
            SetUp();
        }

        private void SetUp()
        {
            SetUpDefines();
        }

        private void SetUpDefines()
        {
            AddDefine("int8", "char");
            AddDefine("int16", "short");
            AddDefine("int32", "int");
            AddDefine("int64", "long long");

            AddDefine("uint1", "unsigned char");
            AddDefine("uint8", "unsigned char");
            AddDefine("uint16", "unsigned short");
            AddDefine("uint32", "unsigned int");
            AddDefine("uint64", "unsigned long long");
        }

        private void AddDefine(string symbol, string value)
        {
            Defines.Add((symbol, value));
        }

        private string BuildDefines()
        {
            var defines = new StringBuilder();
            foreach (var (Symbol, Value) in Defines)
                defines.AppendLine($"#define {Symbol} {Value}");

            return defines.ToString();
        }

        public string Build()
        {
            return $"{BuildDefines()}\n{string.Join("\n;", Structures)};\n\n{string.Join("\n", FunctionPrototypes)}\n\n{string.Join("\n", Globals)}\n\n{string.Join("\n", Functions)}";
        }
    }
}