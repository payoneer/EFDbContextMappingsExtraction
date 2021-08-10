using Microsoft.DotNet.Cli.CommandLine;

namespace DbContextMappingDump
{
    internal static class Json
    {
        public static CommandOption ConfigureOption(CommandLineApplication command)
            => command.Option("--json", "Show JSON output");

        public static string Literal(string text)
            => text != null
                ? "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
                : "null";

        public static string Literal(bool? value)
            => value.HasValue
                ? value.Value
                    ? "true"
                    : "false"
                : "null";
    }
}
