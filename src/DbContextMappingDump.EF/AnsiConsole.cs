using System;

namespace DbContextMappingDump
{
    internal static class AnsiConsole
    {
        public static readonly AnsiTextWriter _out = new AnsiTextWriter(Console.Out);

        public static void WriteLine(string text)
            => _out.WriteLine(text);
        public static void Write(string text)
            => _out.Write(text);
    }
}
