using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.DotNet.Cli.CommandLine;
using DbContextMappingDump.Commands;

namespace DbContextMappingDump
{
    internal static class Program
    {
        private const string ResourcesAssemblySuffix = ".resources";

        private static int Main(string[] args)
        {
            if (args.Any(x => x.Contains("--debug")))
            {
                Console.WriteLine("WaitingForDebuggerToAttach");
                Console.WriteLine($"ProcessId {Process.GetCurrentProcess().Id}");
                Console.ReadLine();
            }

            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;


            if (Console.IsOutputRedirected)
            {
                Console.OutputEncoding = Encoding.UTF8;
            }

            var app = new CommandLineApplication { Name = "DbContextMappingDump" };

            new RootCommand().Configure(app);

            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
                var wrappedException = ex as WrappedException;
                if (ex is CommandException
                    || ex is CommandParsingException
                    || (wrappedException?.Type == "Microsoft.EntityFrameworkCore.Design.OperationException"))
                {
                    Reporter.WriteVerbose(ex.ToString());
                }
                else
                {
                    Reporter.WriteInformation(ex.ToString());
                }

                Reporter.WriteError(ex.Message);

                return 1;
            }
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            var asmPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), name + ".dll");

            if (!File.Exists(asmPath) && 
                name.EndsWith(ResourcesAssemblySuffix))
            {
                name = name.Substring(0, name.Length - ResourcesAssemblySuffix.Length);
                asmPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), name + ".dll");
            }

            Console.WriteLine($"AssemblyResolve {args.Name}  {asmPath} {args.RequestingAssembly}");
            return Assembly.LoadFrom(asmPath);
        }
    }
}
