using Microsoft.DotNet.Cli.CommandLine;

namespace DbContextMappingDump.Commands
{
    internal class ProjectCommandBase : EFCommandBase
    {
        public override void Configure(CommandLineApplication command)
        {
            new ProjectOptions().Configure(command);

            base.Configure(command);
        }
    }
}
