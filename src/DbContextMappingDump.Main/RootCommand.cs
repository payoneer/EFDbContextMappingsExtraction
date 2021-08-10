using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using DbContextMappingDump.Infra.DataContracts;
using Microsoft.DotNet.Cli.CommandLine;
using DbContextMappingDump.Commands;
using EFCommand = DbContextMappingDump.Commands.RootCommand;

namespace DbContextMappingDump
{
    internal class RootCommand : CommandBase
    {
        public const string JsonResultHeader = "JsonResult:";
        public const string JsonResultFooter = "JsonResult Done";

        private CommandLineApplication _command;
        private ProjectOptions _options;
        private CommandOption _help;
        private IList<string> _args;
        private IList<string> _applicationArgs;

        public string EfCoreToolLocation { get; }
        public string FullNetToolLocation { get; }

        public RootCommand() :
            this(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "netcoreapp2.0"),
                Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "net461"))
        {

        }
        public RootCommand(string efCoreToolLocation, string fullNetToolLocation)
        {
            EfCoreToolLocation = efCoreToolLocation;
            FullNetToolLocation = fullNetToolLocation;
        }
        public override void Configure(CommandLineApplication command)
        {
            command.FullName = $"DotnetEfFullName";
            command.AllowArgumentSeparator = true;

            var options = new ProjectOptions();
            options.Configure(command);

            _options = options;


            command.VersionOption("--version", GetVersion);
            _help = command.Option("-h|--help", description: null);

            _args = command.RemainingArguments;
            _applicationArgs = command.ApplicationArguments;

            base.Configure(command);

            _command = command;
        }

        protected override int Execute(string[] _)
        {
            var commands = _args.TakeWhile(a => a[0] != '-').ToList();
            if (_help.HasValue()
                || ShouldHelp(commands))
            {
                return ShowHelp(_help.HasValue(), commands);
            }

            var targetDir = "";
            var targetPath = "";
            var startupTargetPath = "";
            var targetFramework = ParseNETVersion(_options.Framework.HasValue() ? _options.Framework.Value() : ".NETCoreApp");
            var projectAssetsFile = "";

            if (_options.ContextAssembly.HasValue())
            {
                targetDir = Path.GetDirectoryName(Path.GetFullPath(_options.StartupAssembly.HasValue() ? _options.StartupAssembly.Value() : _options.ContextAssembly.Value()));
                targetPath = _options.ContextAssembly.Value();
                startupTargetPath = _options.StartupAssembly.Value();

            }
            else
            {
                var (projectFile, startupProjectFile) = ResolveProjects(
                    _options.Project.Value(),
                    _options.StartupProject.Value());


                var project = Project.FromFile(projectFile, _options.MSBuildProjectExtensionsPath.Value());
                var startupProject = Project.FromFile(
                    startupProjectFile,
                    _options.MSBuildProjectExtensionsPath.Value(),
                    _options.Framework.Value(),
                    _options.Configuration.Value(),
                    _options.Runtime.Value());

                if (!_options.NoBuild.HasValue())
                {
                    startupProject.Build();
                }

                targetDir = Path.GetFullPath(Path.Combine(startupProject.ProjectDir, startupProject.OutputPath));
                targetPath = Path.Combine(targetDir, project.TargetFileName);
                startupTargetPath = Path.Combine(targetDir, startupProject.TargetFileName);
                targetFramework = ParseNETVersion(_options.Framework.HasValue() ? _options.Framework.Value() : new FrameworkName(startupProject.TargetFrameworkMoniker).Identifier);
                projectAssetsFile = startupProject.ProjectAssetsFile;
            }

            var args = new List<string>();
            var efVarsion = Enum.Parse<EFVersion>(_options.EFVersion.Value());
            var result = ExtractMappings(targetPath, startupTargetPath, efVarsion, targetFramework,_options.Json.HasValue(), DebugMode.HasValue());
            return result.exitCode;
        }

        private NETVersion ParseNETVersion(string framework)
        {
            if (framework.Contains("NETFramework", StringComparison.InvariantCultureIgnoreCase))
            {
                return NETVersion.NETFramework;
            }
            return NETVersion.NETCore;
        }

        private static (string, string) ResolveProjects(
            string projectPath,
            string startupProjectPath)
        {
            var projects = ResolveProjects(projectPath);
            var startupProjects = ResolveProjects(startupProjectPath);

            if (projects.Count > 1)
            {
                throw new CommandException(
                    projectPath != null
                        ? $"MultipleProjectsInDirectory(projectPath)"
                        : $"MultipleProjects");
            }

            if (startupProjects.Count > 1)
            {
                throw new CommandException(
                    startupProjectPath != null
                        ? $"MultipleProjectsInDirectory(startupProjectPath)"
                        : $"MultipleStartupProjects");
            }

            if (projectPath != null
                && projects.Count == 0)
            {
                throw new CommandException($"NoProjectInDirectory(projectPath)");
            }

            if (startupProjectPath != null
                && startupProjects.Count == 0)
            {
                throw new CommandException($"NoProjectInDirectory(startupProjectPath)");
            }

            if (projectPath == null
                && startupProjectPath == null)
            {
                return projects.Count == 0
                    ? throw new CommandException($"NoProject")
                    : (projects[0], startupProjects[0]);
            }

            if (projects.Count == 0)
            {
                return (startupProjects[0], startupProjects[0]);
            }

            if (startupProjects.Count == 0)
            {
                return (projects[0], projects[0]);
            }

            return (projects[0], startupProjects[0]);
        }

        private static List<string> ResolveProjects(string path)
        {
            if (path == null)
            {
                path = Directory.GetCurrentDirectory();
            }
            else if (!Directory.Exists(path)) // It's not a directory
            {
                return new List<string> { path };
            }

            var projectFiles = Directory.EnumerateFiles(path, "*.*proj", SearchOption.TopDirectoryOnly)
                .Where(f => !string.Equals(Path.GetExtension(f), ".xproj", StringComparison.OrdinalIgnoreCase))
                .Take(2).ToList();

            return projectFiles;
        }

        private static string GetVersion()
            => typeof(RootCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

        private static bool ShouldHelp(IReadOnlyList<string> commands)
            => commands.Count == 0
               || (commands.Count == 1
                   && (commands[0] == "database"
                       || commands[0] == "dbcontext"
                       || commands[0] == "migrations"));

        private int ShowHelp(bool help, IEnumerable<string> commands)
        {
            var app = new CommandLineApplication { Name = _command.Name };

            new EFCommand().Configure(app);

            app.FullName = _command.FullName;

            var args = new List<string>(commands);
            if (help)
            {
                args.Add("--help");
            }

            return app.Execute(args.ToArray());
        }


        public (int exitCode, string output) ExtractMappings(string dbContextAssemblyPath, string startupAssemblyPath = "",
           EFVersion eFVersion = EFVersion.EFCore,
           NETVersion netVersion = NETVersion.NETCore,
           bool outputInJson = true,
           bool startInDebugMode = false)
        {
            var targetDir = "";
            var targetPath = "";
            var startupTargetPath = "";
            var projectAssetsFile = "";


            targetDir = Path.GetDirectoryName(Path.GetFullPath(!string.IsNullOrEmpty(startupAssemblyPath) ? startupAssemblyPath : dbContextAssemblyPath));
            targetPath = dbContextAssemblyPath;
            startupTargetPath = startupAssemblyPath;



            string executable;
            var args = new List<string>();
            var startupProjectAssemblyName = Path.GetFileNameWithoutExtension(startupTargetPath);
            var toolsPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);



            var depsFile = Path.Combine(
                targetDir,
                startupProjectAssemblyName + ".deps.json");
            var runtimeConfig = Path.Combine(
                targetDir,
                startupProjectAssemblyName + ".runtimeconfig.json");



            if (netVersion == NETVersion.NETFramework)
            {
                executable = Path.Combine(
                    FullNetToolLocation,
                    "DbContextMappingDump.EF.exe");
            }
            else if (netVersion == NETVersion.NETCore)
            {
                var efToolPath = EfCoreToolLocation;

                executable = "dotnet";
                args.Add("exec");
                if (File.Exists(depsFile))
                {
                    args.Add("--depsfile");
                    args.Add(depsFile);
                }


                args.Add("--additional-deps");
                args.Add(Path.Combine(efToolPath, @"DbContextMappingDump.EF.deps.json"));

                args.Add("--additionalprobingpath");
                args.Add(efToolPath.TrimEnd(Path.DirectorySeparatorChar));

                var dumpToolAssestsFile = Path.Combine(efToolPath, "DbContextMappingDump.EF.runtimeconfig.dev.json");
                if (File.Exists(dumpToolAssestsFile))
                {
                    using (var reader = JsonDocument.Parse(File.OpenRead(dumpToolAssestsFile)))
                    {
                        var projectAssets = reader.RootElement;
                        var packageFolders = projectAssets.GetProperty("runtimeOptions").GetProperty("additionalProbingPaths").EnumerateArray().Select(s => s.GetString());

                        foreach (var packageFolder in packageFolders)
                        {
                            args.Add("--additionalprobingpath");
                            args.Add("\"" + packageFolder.TrimEnd(Path.DirectorySeparatorChar) + "\"");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(projectAssetsFile))
                {
                    using (var reader = JsonDocument.Parse(File.OpenRead(projectAssetsFile)))
                    {
                        var projectAssets = reader.RootElement;
                        var packageFolders = projectAssets.GetProperty("packageFolders").EnumerateObject().Select(p => p.Name);

                        foreach (var packageFolder in packageFolders)
                        {
                            args.Add("--additionalprobingpath");
                            args.Add("\"" + packageFolder.TrimEnd(Path.DirectorySeparatorChar) + "\"");
                        }
                    }
                }

                if (File.Exists(runtimeConfig))
                {
                    args.Add("--runtimeconfig");
                    args.Add(runtimeConfig);
                }


                args.Add(Path.Combine(efToolPath, @"DbContextMappingDump.EF.dll"));
            }
            else
            {
                throw new Exception(
                    $"UnsupportedFramework(startupProject.ProjectName, targetFramework.Identifier)");
            }
            args.Add("dbcontext");
            args.Add("mappings");
            args.Add("--assembly");
            args.Add(targetPath);
            if (!string.IsNullOrEmpty(startupAssemblyPath))
            {
                args.Add("--startup-assembly");
                args.Add(startupTargetPath);
            }

            args.Add("--working-dir");
            args.Add(Directory.GetCurrentDirectory());
            args.Add("--ef-version");
            args.Add(eFVersion.ToString());

            if (outputInJson)
            {
                args.Add("--json");
            }

            if (startInDebugMode)
            {
                args.Add("--debug");
            }

            (int exitCode, string output) result = Exe.Run(executable, args, targetDir, terminationText: JsonResultFooter);

            return result;
        }
    }
}
