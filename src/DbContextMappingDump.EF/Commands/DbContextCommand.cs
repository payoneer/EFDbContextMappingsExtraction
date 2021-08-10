using Microsoft.DotNet.Cli.CommandLine;

namespace DbContextMappingDump.Commands
{
    internal class DbContextCommand : HelpCommandBase
    {
        public override void Configure(CommandLineApplication command)
        {        

            command.Command("mappings", new DbContextMappingsCommand().Configure);
          
            base.Configure(command);
        }
    }
}
