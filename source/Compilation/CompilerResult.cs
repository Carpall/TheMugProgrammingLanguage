using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Compilation
{
    public struct CompilerResult<T>
    {
        public T Value { get; }

        public CompilationException Exception { get; }
        
        public CompilerResult(T value)
        {
            Value = value;
            Exception = null;
        }

        public CompilerResult(CompilationException exception)
        {
            Exception = exception;
            Value = default;
        }

        public CompilerResult(T value, CompilationException exception)
        {
            Value = value;
            Exception = exception;
        }

        public bool IsGood() => Exception is null;
    }
}
