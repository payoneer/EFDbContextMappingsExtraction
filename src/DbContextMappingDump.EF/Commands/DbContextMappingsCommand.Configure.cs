using Microsoft.DotNet.Cli.CommandLine;

namespace DbContextMappingDump.Commands
{
    internal partial class DbContextMappingsCommand : ContextCommandBase
    {
        private CommandOption _json;

        public override void Configure(CommandLineApplication command)
        {
            command.Description = "Return the mapping of entities, properties and methods to tables, columns, stored precedures, functions and sequences";

            _json = Json.ConfigureOption(command);

            base.Configure(command);
        }
    }
}
