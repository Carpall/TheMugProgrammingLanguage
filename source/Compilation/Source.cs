using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mug.Compilation
{
    public struct Source
    {
        public string Filename { get; }

        public string Code { get; }

        public Source(string filename, string source)
        {
            Filename = filename;
            Code = source;
        }

        public static Source ReadFromPath(string path)
        {
            return new(path, File.ReadAllText(path));
        }
    }
}
