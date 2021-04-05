using Mug.Models.Lexer;
using Pastel;
using System;
using System.Drawing;

namespace Mug.Compilation
{
    public static class CompilationErrors
    {
        /// <summary>
        /// general compilation-error that have no position in the text to report,
        /// for example "no argument passed" error
        /// </summary>
        public static void Throw(string error)
        {
            throw new CompilationException(error);
        }

        public static void Throw(this MugLexer Lexer, int pos, string error)
        {
            Lexer.Throw(pos..(pos + 1), error);
        }

        public static void Throw(this MugLexer Lexer, Token token, string error)
        {
            Lexer.Throw(token.Position, error);
        }

        public static void Throw(this MugLexer Lexer, Range position, string error)
        {
            Lexer.DiagnosticBag.Report(new(position, error));
            throw new CompilationException(Lexer);
        }

        /// <summary>
        /// returns the line number of a char index in the text
        /// </summary>
        public static int CountLines(string source, int posStart)
        {
            int count = 1;
            for (; posStart >= 0; posStart--)
                if (source[posStart] == '\n')
                    count++;

            return count;
        }

        private static string GetLine(string source, ref int index)
        {
            var result = "";

            var counter = index;

            while (counter > 0) {
                if (source[counter] == '\n') {
                    counter += 1;
                    break;
                }

                counter -= 1;
            }

            index -= counter;

            while (counter < source.Length && source[counter] != '\n') {
                result += source[counter];
                counter += 1;
            }

            return result;
        }

        /// <summary>
        /// pretty module info printing
        /// </summary
        public static void WriteModule(string moduleName, int lineAt)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(lineAt);
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write("; ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(moduleName);
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("]");
            Console.ResetColor();
        }

        /// <summary>
        /// pretty module info printing
        /// </summary
        public static void WriteModuleStyle(string moduleName, int lineAt, int column, string error = "")
        {
            Console.Write($" ---> {moduleName.Pastel(Color.GreenYellow)}{(lineAt > 0 ? $"{"(".Pastel(Color.HotPink)}{lineAt}{":".Pastel(Color.HotPink)}{column}{")".Pastel(Color.HotPink)}" : "")}");
            Console.WriteLine(error != "" ? $": {error.Pastel(Color.Orange)}" : "");
        }

        public static void GetColumn(Range position, ref string source, out int start, out int end)
        {
            start = position.Start.Value;
            source = GetLine(source, ref start);
            end = position.End.Value - (position.Start.Value - start);
        }

        /// <summary>
        /// pretty error printing
        /// </summary>
        public static void WriteSourceLine(Range position, int lineAt, string source, string error)
        {
            GetColumn(position, ref source, out var start, out var end);

            Console.Write(lineAt);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(" | ");
            Console.ResetColor();
            Console.Write(source[..start].Replace("\t", " "));
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(source[start..end].Replace("\t", " "));
            Console.ResetColor();
            Console.Write("{0}\n{1} ", source[end..].Replace("\t", " "), new string(' ', lineAt.ToString().Length + 3 + source[..start].Length)
                + new string('-', source[start..end].Length));
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(error);
            Console.ResetColor();
        }

        /// <summary>
        /// pretty error printing
        /// </summary>
        public static void WriteSourceLineStyle(string modulename, Range position, int lineAt, string source, string error)
        {
            GetColumn(position, ref source, out var start, out var end);

            WriteModuleStyle(modulename, lineAt, start, error);

            var space = new string(' ', lineAt.ToString().Length);

            Console.WriteLine($"{space}  {"|".Pastel(Color.DeepPink)}");
            Console.Write($" {lineAt} {"|".Pastel(Color.Red)}  ");
            Console.Write(source[..start].Replace("\t", " "));
            Console.Write(source[start..end].Replace("\t", " ").Pastel(Color.Red));
            Console.Write(
                @$"{source[end..].Replace("\t", " ")}
{space}  {"|".Pastel(Color.DeepPink)}{' ', -4}{new string('-', source[start..end].Length).Pastel(Color.Cyan)}

");
        }

        /// <summary>
        /// pretty general error printing
        /// </summary>
        public static void WriteFail(string modulename, string error)
        {
            if (modulename != "")
                WriteModuleStyle(modulename, 0, 0);

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("Error");
            Console.ResetColor();
            Console.WriteLine(": " + string.Join("", error));
        }
    }
}
