using Mug.AstGeneration.IR.Values.Instructions;
using Mug.AstGeneration.IR.Values.Typing;
using Mug.Compilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR.Values
{
    public interface ILiquorValue
    {
        public ILiquorType Type { get; set; }
        public ModulePosition Position { get; }

        public string WriteToString()
        {
            return $"{Type} {this}";
        }
    }
}
