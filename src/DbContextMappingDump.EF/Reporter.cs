using System;
using System.Linq;
using static DbContextMappingDump.AnsiConstants;

namespace DbContextMappingDump
{
    internal static class Reporter
    {
        public static bool IsVerbose { get; set; }
        public static bool NoColor { get; set; }
        public static bool PrefixOutput { get; set; } = false;

        public static string Colorize(string value, Func<string, string> colorizeFunc)
            => NoColor ? value : colorizeFunc(value);

        public static void WriteError(string message)
            => WriteLine(Prefix("error:   ", Colorize(message, x => Bold + Red + x + Reset)));

        public static void WriteWarning(string message)
            => WriteLine(Prefix("warn:    ", Colorize(message, x => Bold + Yellow + x + Reset)));

        public static void WriteInformation(string message)
            => WriteLine(Prefix("info:    ", message));

        public static void WriteDataLine(string message, int indent=0)
            => WriteLine(Prefix("data:    ", Colorize(message, x => Bold + Gray + x + Reset)), indent);

        public static void WriteData(string message, int indent = 0)
            => Write(Prefix("data:    ", Colorize(message, x => Bold + Gray + x + Reset)), indent);


        public static void WriteVerbose(string message)
        {
            if (IsVerbose)
            {
                WriteLine(Prefix("verbose: ", Colorize(message, x => Bold + Black + x + Reset)));
            }
        }

        private static string Prefix(string prefix, string value)
            => PrefixOutput
                ? string.Join(
                    Environment.NewLine,
                    value.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(l => prefix + l))
                : value;

        private static void WriteLine(string value, int indent=0)
        {
            value = value.PadLeft(indent + value.Length);

            if (NoColor)
            {
                Console.WriteLine(value);
            }
            else
            {
                AnsiConsole.WriteLine(value);
            }
        }

        private static void Write(string value, int indent = 0)
        {
            value = value.PadLeft(indent + value.Length);

            if (NoColor)
            {
                Console.Write(value);
            }
            else
            {
                AnsiConsole.Write(value);
            }
        }
    }
}
