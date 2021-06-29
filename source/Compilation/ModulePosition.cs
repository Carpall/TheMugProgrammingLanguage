using Mug.Grammar;
using Newtonsoft.Json;
using System;

namespace Mug.Compilation
{
    public struct ModulePosition
    {
        [JsonIgnore]
        public Source Source { get; }
        public Range Position { get; }

        public ModulePosition(Source source, Range position)
        {
            Source = source;
            Position = position;
        }

        public int LineAt()
        {
            return PrettyPrinter.CountLines(Source.Code, Position.Start.Value);
        }
    }
}
