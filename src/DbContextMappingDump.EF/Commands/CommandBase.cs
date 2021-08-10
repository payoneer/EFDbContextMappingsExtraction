using Microsoft.DotNet.Cli.CommandLine;
using System;
using System.Diagnostics;

namespace DbContextMappingDump.Commands
{
    internal abstract class CommandBase
    {
        private CommandOption _debugMode;

        internal CommandOption DebugMode { get => _debugMode; set => _debugMode = value; }

        public virtual void Configure(CommandLineApplication command)
        {
            var verbose = command.Option("-v|--verbose", "");
            DebugMode = command.Option("-d|--debug", "allow attaching to process for debugging purposes");

            var noColor = command.Option("--no-color", "");

            command.HandleResponseFiles = true;

            command.OnExecute(
                (args) =>
                {
                    Reporter.IsVerbose = verbose.HasValue();
                    Reporter.NoColor = noColor.HasValue();
                    
                    Validate();

                    return Execute(args);
                });
        }

        protected virtual void Validate()
        {
        }

        protected virtual int Execute( string[] args)
            => 0;
    }
}
