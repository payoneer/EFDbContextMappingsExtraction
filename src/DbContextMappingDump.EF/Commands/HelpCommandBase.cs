using Microsoft.DotNet.Cli.CommandLine;

namespace DbContextMappingDump.Commands
{
    internal class HelpCommandBase : EFCommandBase
    {
        private CommandLineApplication _command;

        public override void Configure(CommandLineApplication command)
        {
            _command = command;

            base.Configure(command);
        }

        protected override int Execute(string[] args)
        {
            _command.ShowHelp();

            return base.Execute(args);
        }
    }
}
