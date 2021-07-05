using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Typing
{
    public class BadType : IType
    {
        public override string ToString()
        {
            return "BadType";
        }
    }
}
