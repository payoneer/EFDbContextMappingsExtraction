using System;
using System.IO;
using System.Reflection;
using DbContextMappingDump.Infra.DataContracts;
using Microsoft.DotNet.Cli.CommandLine;
namespace DbContextMappingDump.Commands
{
    internal abstract class ProjectCommandBase : EFCommandBase
    {
        private CommandOption _assembly;
        private CommandOption _startupAssembly;
        private CommandOption _dataDir;
        private CommandOption _projectDir;
        private CommandOption _rootNamespace;
        private CommandOption _language;

        protected CommandOption WorkingDir { get; private set; }
        protected CommandOption EFVersion { get; private set; }

        public override void Configure(CommandLineApplication command)
        {
            command.AllowArgumentSeparator = true;

            _assembly = command.Option("-a|--assembly <PATH>", "assembly of the DbContext");
            _startupAssembly = command.Option("-s|--startup-assembly <PATH>", "Assembly where the Program.Main of the app is defined");
            _dataDir = command.Option("--data-dir <PATH>", "");
            _projectDir = command.Option("--project-dir <PATH>", "");
            _rootNamespace = command.Option("--root-namespace <NAMESPACE>", "");
            _language = command.Option("--language <LANGUAGE>", "");
            WorkingDir = command.Option("--working-dir <PATH>", "");
            EFVersion = command.Option("--ef-version <EF6/EFCore>", "");

            base.Configure(command);
        }

        protected override void Validate()
        {
            base.Validate();


        }

        protected IOperationExecutor CreateExecutor(string[] remainingArguments)
        {
            try
            {
#if NET461_OR_GREATER
                if (EFVersion.Value() == "EFCore")
                {
                    return new ReflectionOperationExecutor(
                                    _assembly.Value(),
                                    _startupAssembly.Value(),
                                    _projectDir.Value(),
                                    _dataDir.Value(),
                                    _rootNamespace.Value(),
                                    _language.Value(),
                                    remainingArguments);
                }

                return new EF6OperationExecutor(_assembly.Value(),
                    _startupAssembly.Value(),
                    _projectDir.Value(),
                    _dataDir.Value(),
                    _rootNamespace.Value(),
                    _language.Value(),
                    remainingArguments);
#else
                if (EFVersion.Value() == "EFCore")
                {
                    return new ReflectionOperationExecutor(
                                        _assembly.Value(),
                                        _startupAssembly.Value(),
                                        _projectDir.Value(),
                                        _dataDir.Value(),
                                        _rootNamespace.Value(),
                                        _language.Value(),
                                        remainingArguments);
                }
                else
                {
                    throw new NotSupportedException(".NET Core doesnt support EF version other than EFCore");
                }
#endif

            }
            catch (FileNotFoundException)
            {
                throw;
            }
        }
    }
}
