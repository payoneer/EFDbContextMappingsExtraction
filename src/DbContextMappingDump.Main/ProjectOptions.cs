using Microsoft.DotNet.Cli.CommandLine;namespace DbContextMappingDump
{
    internal class ProjectOptions
    {
        public CommandOption Project { get; private set; }
        public CommandOption StartupProject { get; private set; }
        public CommandOption ContextAssembly { get; private set; }
        public CommandOption StartupAssembly { get; private set; }
        public CommandOption Framework { get; private set; }
        public CommandOption Configuration { get; private set; }
        public CommandOption Runtime { get; private set; }
        public CommandOption Json { get; private set; }
        public CommandOption MSBuildProjectExtensionsPath { get; private set; }
        public CommandOption NoBuild { get; private set; }
        public CommandOption EFVersion { get; private set; }


        public void Configure(CommandLineApplication command)
        {
            Project = command.Option("-p|--project <PROJECT>", "The project to use. Defaults to the current working directory.");
            StartupProject = command.Option("-s|--startup-project <PROJECT>", "The startup project to use. Defaults to the current working directory.");
            ContextAssembly = command.Option("--context-assembly <assembly>", "path to the assemmly containing the dbcontext");
            StartupAssembly = command.Option("--startup-assembly <assembly>", "path to the assemmly containing the startup");
            Framework = command.Option("--framework <FRAMEWORK>", ".NETCoreApp | .NETFramework");
            EFVersion = command.Option("--ef-version <EFVersion>", "EF6 | EFCore");
            Configuration = command.Option("--configuration <CONFIGURATION>", "");
            Runtime = command.Option("--runtime <RUNTIME_IDENTIFIER>", "");
            Json = command.Option("--json", "Show JSON output");
            MSBuildProjectExtensionsPath = command.Option("--msbuildprojectextensionspath <PATH>", "");
            NoBuild = command.Option("--no-build", "");
        }
    }
}
