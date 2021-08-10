using DbContextMappingDump.Infra.DataContracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DbContextMappingDump
{
    public abstract class OperationExecutorBase : IOperationExecutor
    {
        public const string DesignAssemblyName = "Microsoft.EntityFrameworkCore.Design";
        protected const string ExecutorTypeName = "Microsoft.EntityFrameworkCore.Design.OperationExecutor";

        private static readonly IDictionary _emptyArguments = new Dictionary<string, object>(0);
        public string AppBasePath { get; }

        protected string AssemblyFileName { get; set; }
        protected string StartupAssemblyFileName { get; set; }
        public string ContextAssembly { get; }
        public string StartupAssembly { get; }
        protected string ProjectDirectory { get; }
        protected string RootNamespace { get; }
        protected string Language { get; }
        protected string[] RemainingArguments { get; }
        protected Assembly StartupAssemblyObj { get; private set; }
        protected Assembly ContextAssemblyObj { get; private set; }

        public static string DefaultConnectionString
        {
            get
            {
                string exeLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string connectionString = File.ReadAllLines(
                                                    Path.Combine(
                                                        exeLocation,
                                                        "defaultConnectionString.setting"))[0];
                return connectionString.Replace("{{TOOL_LOCATION}}", exeLocation);
            }
        }
        protected OperationExecutorBase(
            string assembly,
            string startupAssembly,
            string projectDir,
            string rootNamespace,
            string language,
            string[] remainingArguments)
        {
            AssemblyFileName = Path.GetFileNameWithoutExtension(assembly);
            StartupAssemblyFileName = startupAssembly == null
                ? AssemblyFileName
                : Path.GetFileNameWithoutExtension(startupAssembly);

            AppBasePath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(startupAssembly ?? assembly)));

            RootNamespace = rootNamespace ?? AssemblyFileName;
            ContextAssembly = assembly;
            StartupAssembly = startupAssembly;
            ProjectDirectory = projectDir ?? Directory.GetCurrentDirectory();
            Language = language;
            RemainingArguments = remainingArguments ?? Array.Empty<string>();

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            StartupAssemblyObj = Assembly.Load(StartupAssemblyFileName);
            ContextAssemblyObj = Assembly.Load(AssemblyFileName);

        }

        public virtual void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;

        }

        public abstract DbContextMappings GetContextMappings();
        protected Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {            
            var assemblyName = new AssemblyName(args.Name);

            var basePaths = new List<string>()
            {
                AppBasePath,
                Path.GetDirectoryName(StartupAssembly),
                Path.GetDirectoryName(ContextAssembly),
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            };
            //if (assemblyName.Name.Contains(DesignAssemblyName))
            //{
            //    basePaths.Insert(0,@"C:\Users\tamirdr\source\repos\tamirdresher\efcore\artifacts\bin\EFCore.Design\Debug\netstandard2.1");
            //}
            foreach (var basePath in basePaths)
            {
                foreach (var extension in new[] { ".dll", ".exe" })
                {
                    var path = Path.Combine(basePath, assemblyName.Name + extension);
                    if (File.Exists(path))
                    {
                        try
                        {
                            return Assembly.LoadFrom(path);
                        }
                        catch
                        {
                        }
                    }
                }

            }

            return null;
        }

        protected static IEnumerable<TypeInfo> GetConstructibleTypes(Assembly assembly)
                => GetLoadableDefinedTypes(assembly).Where(
                    t => !t.IsAbstract
                        && !t.IsGenericTypeDefinition);

        protected static IEnumerable<TypeInfo> GetLoadableDefinedTypes(Assembly assembly)
        {
            try
            {
                return assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).Select(IntrospectionExtensions.GetTypeInfo);
            }
        }

        protected static IEnumerable<Type> FindDbContextTypes(List<TypeInfo> types, Type dbContextBaseType)
        {
            return types.Where(t => dbContextBaseType.IsAssignableFrom(t)).Select(
                    t => t.AsType())
                .Distinct();
        }

        protected static string FlattenExceptionMessages(Exception ex)
        {
            var error = new StringBuilder();
            for (Exception currEx = ex; currEx != null; currEx = currEx.InnerException)
            {
                error.AppendLine(currEx.Message);
            }

            return error.ToString();
        }

    }
}
