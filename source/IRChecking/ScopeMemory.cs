using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mug.AstGeneration.IR.Values.Typing;

namespace Mug.IRChecking
{
    public class ScopeMemory
    {
        private Dictionary<string, (ILiquorType Type, bool IsMutable)> _cache = new();

        public ScopeMemory(ScopeMemory scopeMemory)
        {
            _cache = new(scopeMemory._cache);
        }

        public bool IsDeclared(string name, out (ILiquorType Type, bool IsMutable) result)
        {
            return _cache.TryGetValue(name, out result);
        }

        public void Declare(string name, ILiquorType type, bool isMutable)
        {
            _cache.Add(name, (type, isMutable));
        }
    }
}
