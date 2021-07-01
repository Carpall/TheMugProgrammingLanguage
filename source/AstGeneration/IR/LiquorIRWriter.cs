using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.AstGeneration.IR
{
    class LiquorIRWriter
    {
        private LiquorIR IR { get; }
        private StringBuilder Builder { get; } = new();

        public LiquorIRWriter(LiquorIR ir)
        {
            IR = ir;
        }

        public string WriteToString()
        {
            WriteDeclarations();

            return Builder.ToString();
        }

        private void WriteDeclarations()
        {
            foreach (var declaration in IR.Declarations)
                Builder.AppendLine(declaration.ToString());
        }
    }
}
