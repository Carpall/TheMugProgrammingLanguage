using Mug.Compilation;
using Mug.Models.Generator;
using Mug.Models.Lexer;
using Mug.MugValueSystem;
using Mug.TypeSystem;
using System.Collections.Generic;

namespace Mug.Models.Parser.NodeKinds.Statements
{
  public class EnumStatement : INode
    {
        public string NodeKind => "Enum";
        public Pragmas Pragmas { get; set; }
        public MugType BaseType { get; set; }
        public string Name { get; set; }
        public List<EnumMemberNode> Body { get; set; } = new();
        
        public ModulePosition Position { get; set; }
        public TokenKind Modifier { get; set; }

        public MugValue GetMemberValueFromName(MugValueType enumerated, MugValueType enumeratedBaseType, string name, ModulePosition position, LocalGenerator localgenerator)
        {
            for (int i = 0; i < Body.Count; i++)
                if (Body[i].Name == name)
                    return MugValue.EnumMember(enumerated, localgenerator.ConstToMugConst(Body[i].Value, Body[i].Position, true, enumeratedBaseType).LLVMValue);

            localgenerator.Error(position, $"Enum '{Name}' does not contain a definition for '{name}'");

            throw new(); // unreachable
        }

        public bool ContainsMemberWithName(string name)
        {
            for (int i = 0; i < Body.Count; i++)
                if (Body[i].Name == name)
                    return true;

            return false;
        }
    }
}
