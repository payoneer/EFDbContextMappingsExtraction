using System;
using System.Reflection;
using Microsoft.DotNet.Cli.CommandLine;

namespace DbContextMappingDump.Commands
{
    internal class RootCommand : HelpCommandBase
    {
        public override void Configure(CommandLineApplication command)
        {
            command.Command("dbcontext", new DbContextCommand().Configure);
            command.VersionOption("--version", GetVersion);

            base.Configure(command);
        }

        protected override int Execute(string[] args)
        {
           

            return base.Execute(args);
        }

        private static string GetVersion()
            => typeof(RootCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
    }
}
