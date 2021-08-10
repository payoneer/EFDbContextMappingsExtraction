using Microsoft.DotNet.Cli.CommandLine;

namespace DbContextMappingDump.Commands
{
    internal abstract class EFCommandBase : CommandBase
    {
        public override void Configure(CommandLineApplication command)
        {
            command.HelpOption("-h|--help");

            base.Configure(command);
        }
    }
}
