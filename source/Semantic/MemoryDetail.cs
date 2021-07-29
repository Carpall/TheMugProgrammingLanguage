using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mug.Typing;
using Mug.Typing.Values;
using Mug.Syntax.AST;

namespace Mug.Semantic
{
    public readonly struct MemoryDetail
    {
        public readonly string Name;

        public readonly bool IsMutable;

        public readonly object Value;

        public readonly IType Type;

        public readonly bool IsGlobal;

        public readonly bool NotFound;

        public MemoryDetail(string name, bool isMutable, object value, IType type, bool isGlobal, bool notFound = false)
        {
            Name = name;
            IsMutable = isMutable;
            Value = value;
            Type = type;
            IsGlobal = isGlobal;
            NotFound = notFound;
        }

        internal static MemoryDetail Undeclared => new(default, default, default, default, default, true);
    }
}
