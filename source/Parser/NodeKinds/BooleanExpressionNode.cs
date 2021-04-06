using Mug.Compilation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Mug.Models.Parser.NodeKinds
{
    public class BooleanExpressionNode : INode
    {
        public string NodeKind => "BooleanBinaryExpression";
        public INode Left { get; set; }
        public INode Right { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public OperatorKind Operator { get; set; }
        public ModulePosition Position { get; set; }
    }
}
