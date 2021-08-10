using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DbContextMappingDump;
using DbContextMappingDump.Infra.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using ReflectionMagic;

namespace DbContextMappingDump
{
    public class ReflectionOperationExecutor : OperationExecutorBase
    {
        private const string ReportHandlerTypeName = "Microsoft.EntityFrameworkCore.Design.OperationReportHandler";
        private const string ResultHandlerTypeName = "Microsoft.EntityFrameworkCore.Design.OperationResultHandler";
        private readonly AppServiceProviderFactory _appServicesFactory;

        private Assembly _efCoreAssemblyObj;
        private readonly Assembly _efCoreRelationalAssembly;
        private readonly Assembly _efCoreSqlServerAssembly;

        public ReflectionOperationExecutor(
            string assembly,
            string startupAssembly,
            string projectDir,
            string dataDirectory,
            string rootNamespace,
            string language,
            string[] remainingArguments)
            : base(assembly, startupAssembly, projectDir, rootNamespace, language, remainingArguments)
        {
            if (dataDirectory != null)
            {
                Reporter.WriteVerbose("Resources.UsingDataDir(dataDirectory)");
                AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
            }

            _efCoreAssemblyObj = Assembly.Load("Microsoft.EntityFrameworkCore");
            _efCoreRelationalAssembly = Assembly.Load("Microsoft.EntityFrameworkCore.Relational");
            _efCoreSqlServerAssembly = Assembly.Load(" Microsoft.EntityFrameworkCore.SqlServer");
            _appServicesFactory = new AppServiceProviderFactory(StartupAssemblyObj);


        }
        private IDictionary<Type, Func<object>> FindContextTypes(Assembly startupAssembly, Assembly contextAssembly, Assembly efCoreAssembly)
        {

            var contexts = new Dictionary<Type, Func<object>>();


            //Look for DbContext classes registered in the service provider

            var appServices = _appServicesFactory.Create(RemainingArguments);
            var dbContextOptionsType = efCoreAssembly.GetType("Microsoft.EntityFrameworkCore.DbContextOptions");
            try
            {
                var registeredContexts = appServices.GetServices(dbContextOptionsType)
                       .Select(o => (Type)((dynamic)o).ContextType);
                foreach (var context in registeredContexts.Where(c => !contexts.ContainsKey(c)))
                {
                    contexts.Add(
                        context,
                        FindContextFactory(context, efCoreAssembly)
                        ?? FindContextFromRuntimeDbContextFactory(appServices, context, efCoreAssembly)
                        ?? (() => CreateDbContext(appServices, context)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            var provider = appServices;

            // Look for DbContext classes in assemblies
            var types = GetConstructibleTypes(startupAssembly)
                .Concat(GetConstructibleTypes(contextAssembly))
                .ToList();

            var dbContextBaseType = efCoreAssembly.GetType("Microsoft.EntityFrameworkCore.DbContext");
            IEnumerable<Type> contextTypes = FindDbContextTypes(types, dbContextBaseType);

            foreach (var context in contextTypes.Where(c => !contexts.ContainsKey(c)))
            {
                contexts.Add(
                    context,
                    () =>
                    {
                        return CreateDbContext(provider, context);
                    });
            }

            return contexts;
        }

        private object CreateDbContext(IServiceProvider provider, Type context)
        {
            StringBuilder errors = new StringBuilder();

#if NETFRAMEWORK
            TrySetConnectionStringForContext(context);
#endif

            try
            {
                return ActivatorUtilities.GetServiceOrCreateInstance(provider, context);
            }
            catch (Exception ex)
            {
                errors.AppendLine(ex.Message);
                object[] ctorArgs = new object[] { DefaultConnectionString };
                var instance = TryCreateInstance(context, errors);
                if (instance != null)
                {
                    return instance;
                }

                throw new Exception($"couldnt create instance of {context.Name}. Errors:{errors}", ex);
            }
        }

#if NETFRAMEWORK

        private static void RemoveReadOnlyFromConnectionStringsSettings()
        {
            typeof(System.Configuration.ConfigurationElementCollection)
                .GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(System.Configuration.ConfigurationManager.ConnectionStrings, false);
        }
        void TrySetConnectionStringForContext(Type ctxType)
        {
            try
            {
                RemoveReadOnlyFromConnectionStringsSettings();
                System.Configuration.ConfigurationManager.ConnectionStrings.Remove(ctxType.Name);
            }
            catch { }
            try
            {
                System.Configuration.ConfigurationManager.ConnectionStrings.Add(new System.Configuration.ConnectionStringSettings(ctxType.Name, DefaultConnectionString, "System.Data.SqlClient"));
            }
            catch (Exception) { }
        }
#endif

        private object TryCreateInstance(Type context, StringBuilder errors)
        {
            var ctors = context.GetConstructors().OrderBy(c => c.GetParameters().Length).ToArray();
            foreach (var ctor in ctors)
            {
                try
                {
                    List<object> args = new List<object>();
                    foreach (var parameter in ctor.GetParameters())
                    {
                        var argValue = TrySatisfyParameter(context, parameter);
                        args.Add(argValue);
                    }
                    return ctor.Invoke(args.ToArray());
                }
                catch (Exception ex)
                {
                    errors.AppendLine(FlattenExceptionMessages(ex));
                }
            }
            return null;
        }

        private object TrySatisfyParameter(Type context, ParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(string))
            {
                return DefaultConnectionString;
            }

            var dbContextOptionsType = _efCoreAssemblyObj.GetType("Microsoft.EntityFrameworkCore.DbContextOptions");
            var dbContextOptionsGenericType = _efCoreAssemblyObj.GetType("Microsoft.EntityFrameworkCore.DbContextOptions`1")?.MakeGenericType(context);
            if (parameter.ParameterType == dbContextOptionsType || parameter.ParameterType == dbContextOptionsGenericType)
            {
                var dbContextOptionsBuilderType = _efCoreAssemblyObj.GetType("Microsoft.EntityFrameworkCore.DbContextOptionsBuilder");
                var dbContextOptionsBuilderTypeGeneric = _efCoreAssemblyObj.GetType("Microsoft.EntityFrameworkCore.DbContextOptionsBuilder`1").MakeGenericType(context);
                dynamic dbContextBuilder = Activator.CreateInstance(dbContextOptionsBuilderTypeGeneric);
                var sqlServerBuilderExtensionsType = _efCoreSqlServerAssembly?.GetType("Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions")?.AsDynamicType();

                sqlServerBuilderExtensionsType?.UseSqlServer(dbContextBuilder, DefaultConnectionString, null);
                return dbContextBuilder.Options;
            }


            if (parameter.ParameterType.FullName.Contains("Microsoft.Extensions.Logging.Abstractions.ILogger"))
            {
                var loggerAsm = Assembly.Load("Microsoft.Extensions.Logging.Abstractions");
                var nullLoggerType = loggerAsm.GetType("Microsoft.Extensions.Logging.Abstractions.NullLogger`1")
                    .MakeGenericType(context)
                    .AsDynamicType();
                return nullLoggerType.Instance;
            }


            return GetDefault(parameter.ParameterType);
        }
        private object GetDefault(Type t)
        {
            return GetType().GetMethod(nameof(GetDefaultGeneric)).MakeGenericMethod(t).Invoke(this, null);
        }

        private T GetDefaultGeneric<T>()
        {
            return default(T);
        }

        private Func<object> FindContextFactory(Type contextType, Assembly efCoreAssembly)
        {
            var iDesignTimeDbContextFactoryType = efCoreAssembly.GetTypes().FirstOrDefault(t => t.Name.Contains(".IDesignTimeDbContextFactory"));
            var factoryInterface = iDesignTimeDbContextFactoryType?.MakeGenericType(contextType);
            var factory = GetConstructibleTypes(contextType.Assembly)
                .FirstOrDefault(t => factoryInterface?.IsAssignableFrom(t) ?? false);
            return factory == null ? (Func<object>)null : (() => CreateContextFromFactory(factory.AsType(), contextType, iDesignTimeDbContextFactoryType));
        }

        private Func<object> FindContextFromRuntimeDbContextFactory(IServiceProvider appServices, Type contextType, Assembly efCoreAssembly)
        {
            var iDesignTimeDbContextFactoryType = efCoreAssembly.GetTypes().FirstOrDefault(t => t.Name.Contains(".IDesignTimeDbContextFactory"));
            var factoryInterface = iDesignTimeDbContextFactoryType?.MakeGenericType(contextType);
            var service = factoryInterface != null ? appServices.GetService(factoryInterface) : null;
            return service == null
                ? (Func<object>)null
                : () => (object)factoryInterface.GetRuntimeMethods().First(mtd => mtd.Name.Contains("CreateDbContext"))
                    ?.Invoke(service, null);
        }

        private object CreateContextFromFactory(Type factory, Type contextType, Type iDesignTimeDbContextFactoryType)
        {

            return (object)iDesignTimeDbContextFactoryType.MakeGenericType(contextType)
                .GetMethod("CreateDbContext", new[] { typeof(string[]) })
                .Invoke(Activator.CreateInstance(factory), new object[] { RemainingArguments });
        }

        public override void Dispose()
            => AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;

        public override DbContextMappings GetContextMappings()
        {
            IDictionary<Type, Func<object>> ctxts = null;
            DbContextMappings dbContextMappings = new DbContextMappings();
            try
            {
                ctxts = FindContextTypes(StartupAssemblyObj, ContextAssemblyObj, _efCoreAssemblyObj);
            }
            catch (Exception ex)
            {
                dbContextMappings.MappingExtractionSucceeded = false;
                dbContextMappings.ErrorDetails = FlattenExceptionMessages(ex);

                return dbContextMappings;
            }



            foreach (var ctxtFactory in ctxts)
            {
                var ctxMappings = new DbContextMapping();
                ctxMappings.DbContextName = ctxtFactory.Key.Name;
                ctxMappings.DbContextFullName = ctxtFactory.Key.FullName;
                dbContextMappings.DbContexts.Add(ctxMappings);
                StringBuilder errors = new StringBuilder();
                try
                {
                    var ctxt = ctxtFactory.Value();
                    using (var ctxtDispoable = ctxt as IDisposable)
                    {
                        dynamic ctxtd = ctxt;

                        var metadataExtensions = new MetadataExtensions(_efCoreRelationalAssembly);

                        ExtractEntityMappings(ctxMappings, ctxtd, metadataExtensions);
                        ExtractDbFunctionMappings(ctxMappings, ctxtd, errors);
                        ExtractSequenceMappings(ctxMappings, ctxtd, errors);
                    }
                }
                catch (Exception ex)
                {

                    errors.AppendLine(FlattenExceptionMessages(ex));


                }
                if (errors.Length != 0)
                {
                    ctxMappings.MappingExtractionSucceeded = false;
                    ctxMappings.ErrorDetails = errors.ToString();
                }

            }
            return dbContextMappings;
        }

        private void ExtractDbFunctionMappings(DbContextMapping ctxMappings, dynamic ctxtd, StringBuilder errors)
        {
            try
            {
                var annotations = (IEnumerable<dynamic>)ctxtd.Model.GetAnnotations();
                foreach (var annotation in annotations)
                {
                    if (annotation.Value.GetType().Name.EndsWith("DbFunction"))
                    {
                        var dbFunction = annotation.Value;
                        var schema = dbFunction.Schema.ToString();
                        var scehma = string.IsNullOrEmpty(schema) ? dbFunction.DefaultSchema : schema;
                        var functionName = dbFunction.FunctionName.ToString();
                        var method = (MethodInfo)dbFunction.MethodInfo;
                        string methodFullName = $"{method?.ReflectedType?.FullName}.{method?.Name}";

                        ctxMappings.DbFunctions.Add(new DbFunctionMapping()
                        {
                            Schema = schema,
                            Name = functionName,
                            MappedMethodFullName = methodFullName
                        });
                    }
                    else if (annotation.Name.EndsWith("DbFunctions") && annotation.Value is IEnumerable)
                    {
                        var functions = (IEnumerable)annotation.Value;
                        foreach (dynamic functionAnnotation in functions)
                        {
                            var dbFunction = functionAnnotation.Value;
                            var schema = dbFunction.Schema?.ToString() ?? "";
                            var functionName = dbFunction.Name.ToString();
                            var method = (MethodInfo)dbFunction.MethodInfo;
                            string methodFullName = $"{method?.ReflectedType?.FullName}.{method?.Name}";

                            ctxMappings.DbFunctions.Add(new DbFunctionMapping()
                            {
                                Schema = schema,
                                Name = functionName,
                                MappedMethodFullName = methodFullName
                            });
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                errors.AppendLine($"Error while extracting DbFunctions Details: {FlattenExceptionMessages(ex)}");
            }

        }

        private void ExtractSequenceMappings(DbContextMapping ctxMappings, dynamic ctxtd, StringBuilder errors)
        {
            try
            {
                var annotations = (IEnumerable<dynamic>)ctxtd.Model.GetAnnotations();
                foreach (var annotation in annotations)
                {
                    if (annotation.Value.GetType().Name.EndsWith("Sequence"))
                    {
                        AddSequenceMapping(ctxMappings, annotation);
                    }
                    else if (annotation.Name.EndsWith("Sequences") && annotation.Value is IEnumerable)
                    {
                        var seqAnnotations = (IEnumerable)annotation.Value;
                        foreach (dynamic seqAnnotation in seqAnnotations)
                        {
                            AddSequenceMapping(ctxMappings, seqAnnotation);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                errors.AppendLine($"Error while extracting Sequences Details: {FlattenExceptionMessages(ex)}");
            }

            static void AddSequenceMapping(DbContextMapping ctxMappings, dynamic seqAnnotation)
            {
                var sequence = seqAnnotation.Value;
                var schema = sequence.Schema?.ToString() ?? "";                
                var sequenceName = sequence.Name.ToString();

                ctxMappings.Sequences.Add(new SequencenMapping()
                {
                    Schema = schema,
                    Name = sequenceName
                });
            }
        }

        private void ExtractEntityMappings(DbContextMapping ctxMappings, dynamic ctxtd, MetadataExtensions metadataExtensions)
        {
            foreach (var entityType in ctxtd.Model.GetEntityTypes())
            {
                var entityMapping = new EntityMapping() { EntityName = entityType.ClrType.Name.ToString(), EntityFullName = entityType.ClrType.FullName.ToString() };
                ctxMappings.Entities.Add(entityMapping);
                try
                {
                    var tableName = metadataExtensions.GetTableName(entityType);
                    var schemaName = metadataExtensions.GetSchema(entityType);
                    entityMapping.Schema = schemaName;
                    entityMapping.TableName = tableName;
                    foreach (var propertyType in entityType.GetProperties())
                    {
                        var propMapping = new EntityPropertyMapping { PropertyName = propertyType.Name };
                        entityMapping.Properties.Add(propMapping);
                        propMapping.ColumnName = metadataExtensions.GetColumnName(propertyType);

                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
