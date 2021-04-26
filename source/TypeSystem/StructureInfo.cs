using LLVMSharp.Interop;
using Mug.Compilation;
using Mug.Models.Generator;
using Mug.MugValueSystem;

namespace Mug.TypeSystem
{
  public class StructureInfo
    {
        public string Name { get; set; }
        public string[] FieldNames { get; set; }
        public MugValueType[] FieldTypes { get; set; }
        public ModulePosition[] FieldPositions { get; set; }
        public LLVMTypeRef LLVMValue { get; set; }

        public MugValueType GetFieldTypeFromName(string name, IRGenerator generator, ModulePosition position)
        {
            return FieldTypes[GetFieldIndexFromName(name, generator, position)];
        }

        public bool ContainsFieldWithName(string name)
        {
            for (int i = 0; i < FieldNames.Length; i++)
                if (FieldNames[i] == name)
                    return true;

            return false;
        }

        public int GetFieldIndexFromName(string name, IRGenerator generator, ModulePosition position)
        {
            for (int i = 0; i < FieldNames.Length; i++)
                if (FieldNames[i] == name)
                    return i;

            generator.Error(position, $"Undeclared field '{name}'");
            throw new();
        }

        public int Size(int sizeofpointer, IRGenerator generator)
        {
            var result = 0;

            for (int i = 0; i < FieldTypes.Length; i++)
                result += FieldTypes[i].Size(sizeofpointer, generator);

            return result;
        }
    }
}
