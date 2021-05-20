using Mug.Models.Lexer;
using Pastel;
using System;
using System.Drawing;
using System.IO;

namespace Mug.Compilation
{
    public static class PrettyPrinter
    {
        /// <summary>
        /// returns the line number of a char index in the text
        /// </summary>
        public static int CountLines(string source, int posStart)
        {
            var count = 1;
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

        public static void PrintAlerts(CompilationException e)
        {
            if (e.IsGlobalError)
            {
                WriteFail("", e.Message);
                return;
            }

            var i = 0;
            var alerts = e.Diagnostic.GetAlerts();
            try
            {
                for (; i < alerts.Count; i++)
                {
                    var alert = alerts[i];

                    WriteSourceLineStyle(
                        alert.Kind,
                        alert.Bad.Lexer.ModuleName,
                        alert.Bad.Position,
                        alert.Bad.LineAt(),
                        alert.Bad.Lexer.Source,
                        alert.Message);
                }
            }
            catch
            {
                WriteFail(alerts[i].Bad.Lexer.ModuleName, "Internal error: unable to print error message");
            }
        }

        /// <summary>
        /// pretty module info printing
        /// </summary
        public static void WriteModuleStyle(
            string moduleName,
            int lineAt,
            int column,
            CompilationAlertKind kind = CompilationAlertKind.Error,
            Color alertkindColor = new(),
            string alert = "")
        {
            Console.Write($" ---> {Path.GetRelativePath(Environment.CurrentDirectory, moduleName).Pastel(Color.GreenYellow)}{(lineAt > 0 ? $"{"(".Pastel(Color.HotPink)}{lineAt}{":".Pastel(Color.HotPink)}{column}{")".Pastel(Color.HotPink)}" : "")}");
            Console.WriteLine(alert != "" ? $" {kind.ToString().ToLower().Pastel(alertkindColor)}: {alert.Pastel(Color.Orange)}" : "");
        }

        private static Color GetCompilationAlertKindColor(CompilationAlertKind kind)
        {
            return kind switch
            {
                CompilationAlertKind.Error => Color.OrangeRed,
                CompilationAlertKind.Warning => Color.Plum,
                CompilationAlertKind.Note => Color.ForestGreen
            };
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
        public static void WriteSourceLineStyle(
            CompilationAlertKind kind,
            string modulename,
            Range position,
            int lineAt,
            string source,
            string error)
        {
            error = error.Replace("\n", "\\n");

            GetColumn(position, ref source, out var start, out var end);

            var alertkindcolor = GetCompilationAlertKindColor(kind);
            WriteModuleStyle(modulename, lineAt, start, kind, alertkindcolor, error);

            var space = new string(' ', lineAt.ToString().Length);

            Console.WriteLine($"{space}  {"|".Pastel(Color.DeepPink)}");
            Console.Write($" {lineAt} {"|".Pastel(Color.Red)}  ");
            Console.Write(source[..start].Replace("\t", " "));
            Console.Write(source[start..end].Replace("\t", " ").Pastel(Color.OrangeRed));
            Console.Write($"{source[end..].Replace("\t", " ")}\n{space}  {"|".Pastel(Color.DeepPink)}\n\n");
            /*Console.Write(
                @$"{source[end..].Replace("\t", " ")}
{space}  {"|".Pastel(Color.DeepPink)}{new string('-', space.Length - source[start..end].Length).Pastel(Color.Cyan)}

");*/
        }

        /// <summary>
        /// pretty general error printing
        /// </summary>
        public static void WriteFail(string modulename, string error)
        {
            if (modulename != "")
                WriteModuleStyle(modulename, 0, 0);

            Console.Write("Error".Pastel(Color.OrangeRed));
            Console.WriteLine(": " + string.Join("", error));
        }
    }
}
