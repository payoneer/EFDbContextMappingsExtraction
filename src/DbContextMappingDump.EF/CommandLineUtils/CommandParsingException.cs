using System;

namespace Microsoft.DotNet.Cli.CommandLine
{
    internal class CommandParsingException : Exception
    {
        public CommandParsingException(CommandLineApplication command, string message)
            : base(message) => Command = command;

        public CommandLineApplication Command { get; }
    }
}
