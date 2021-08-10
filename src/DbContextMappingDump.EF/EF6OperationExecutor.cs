#if NET461_OR_GREATERusing DbContextMappingDump.Infra.DataContracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbContextMappingDump.Commands
{
    internal class EF6OperationExecutor : OperationExecutorBase
    {
        private AppDomain _domain;

        public EF6OperationExecutor(string assembly,
            string startupAssembly,
            string projectDir,
            string dataDirectory,
            string rootNamespace,
            string language,
            string[] remainingArguments)
            : base(assembly, startupAssembly, projectDir, rootNamespace, language, remainingArguments)
        {
            var info = new AppDomainSetup { ApplicationBase = AppBasePath };

            var configurationFile = (startupAssembly ?? assembly) + ".config";
            if (File.Exists(configurationFile))
            {
                info.ConfigurationFile = configurationFile;
            }

            _domain = AppDomain.CreateDomain("EF6.MappingExtraction", null, info);


            if (dataDirectory != null)
            {
                Reporter.WriteVerbose("Resources.UsingDataDir(dataDirectory)");
                AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
            }

        }

        public override DbContextMappings GetContextMappings()
        {
            var internalEF6OperationExtractor = (InternalEF6OperationExtractor)_domain.CreateInstanceFromAndUnwrap(typeof(InternalEF6OperationExtractor).Assembly.Location,
                 typeof(InternalEF6OperationExtractor).FullName,
                 false,
                 BindingFlags.Default,
                 null,
                 new[]
                 {
                    ContextAssembly,
                    StartupAssembly
                 },
                 null,
                 null
                 );
            return internalEF6OperationExtractor.GetContextMappings();
        }

        public class InternalEF6OperationExtractor : MarshalByRefObject
        {
            protected Assembly StartupAssemblyObj { get; private set; }
            protected Assembly ContextAssemblyObj { get; private set; }
            public string AppBasePath { get; }
            public string AssemblyFileName { get; }
            public string StartupAssemblyFileName { get; }
            public string ContextAssembly { get; }
            public string StartupAssembly { get; }

            StringBuilder _errors = new StringBuilder();
            public InternalEF6OperationExtractor(string assembly, string startupAssembly)
            {
                ContextAssembly = assembly;
                StartupAssembly = startupAssembly;
                AssemblyFileName = Path.GetFileNameWithoutExtension(assembly);
                StartupAssemblyFileName = startupAssembly == null
                    ? AssemblyFileName
                    : Path.GetFileNameWithoutExtension(startupAssembly);
                AppBasePath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(startupAssembly ?? assembly)));
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

                StartupAssemblyObj = Assembly.Load(StartupAssemblyFileName);
                ContextAssemblyObj = Assembly.Load(AssemblyFileName);


            }
            protected Assembly ResolveAssembly(object sender, ResolveEventArgs args)
            {
                var assemblyName = new AssemblyName(args.Name);

                var basePaths = new List<string>()
                {
                    AppBasePath,
                    Path.GetDirectoryName(StartupAssembly),
                    Path.GetDirectoryName(ContextAssembly),
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

            public DbContextMappings GetContextMappings()
            {
                IEnumerable<Type> contextTypes = Enumerable.Empty<Type>();
                var dbContextsMappings = new DbContextMappings() { MappingExtractionSucceeded = false };
                try
                {
                    // Look for DbContext classes in assemblies
                    var types = GetConstructibleTypes(StartupAssemblyObj)
                        .Concat(GetConstructibleTypes(ContextAssemblyObj))
                        .ToList();

                    var dbContextBaseType = typeof(DbContext);
                    contextTypes = FindDbContextTypes(types, dbContextBaseType);
                }
                catch (Exception ex)
                {

                    dbContextsMappings.MappingExtractionSucceeded = false;
                    dbContextsMappings.ErrorDetails = FlattenExceptionMessages(ex);
                    return dbContextsMappings;
                }


                dbContextsMappings.MappingExtractionSucceeded = true;
                foreach (var ctxType in contextTypes)
                {
                    try
                    {
                        TryDisableDatabaseInitializer(ctxType);
                        using (DbContext ctx = CreateDbContext(ctxType))
                        {

                            var res = GetMappings(ctx);
                            dbContextsMappings.DbContexts.Add(res);
                        }

                    }
                    catch (Exception ex)
                    {

                        dbContextsMappings.DbContexts.Add(new DbContextMapping
                        {
                            DbContextName = ctxType.Name,
                            DbContextFullName = ctxType.FullName,
                            MappingExtractionSucceeded = false,
                            ErrorDetails = FlattenExceptionMessages(ex)
                        });
                    }
                }

                return dbContextsMappings;
            }

            private void TryDisableDatabaseInitializer(Type ctxType)
            {
                try
                {
                    typeof(Database).GetMethod(nameof(Database.SetInitializer)).MakeGenericMethod(ctxType).Invoke(null, new object[] { null });
                }
                catch (Exception)
                {

                }
            }

            private DbContext CreateDbContext(Type ctxType)
            {
                TryCreateSetConnectionStringInConfigIfNotExist(ctxType);
                return
                    //(IsEdmxBasedContext(ctxType) ? CreateEdmxConnectionStringAndDbContextWithDefaultCtor(ctxType) : null) ??
                    CreateDbContextWithDefaultConnectionString(ctxType) ??
                    CreateDbContextWithDefaultCtor(ctxType) ??
                    CreateConnectionStringAndDbContextWithDefaultCtor(ctxType) ??
                    CreateEdmxConnectionStringAndDbContextWithDefaultCtor(ctxType) ??
                    throw new Exception($"Constructor on type '{ctxType.Name}' not found. Errors: {_errors.ToString()}");
            }

            private DbContext CreateEdmxConnectionStringAndDbContextWithDefaultCtor(Type ctxType)
            {
                try
                {
                    RemoveReadOnlyFromConnectionStringsSettings();
                    ConfigurationManager.ConnectionStrings.Remove(ctxType.Name);
                }
                catch { }
                try
                {
                    var connectionString = CreateEdmxConnectionStringFor(ctxType);
                    ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings(ctxType.Name, connectionString, "System.Data.SqlClient"));

                }
                catch (Exception ex) { _errors.AppendLine("Couldnt add connection string " + FlattenExceptionMessages(ex)); }

                return CreateDbContextWithDefaultCtor(ctxType);
            }

            private static string CreateEdmxConnectionStringFor(Type ctxType)
            {
                var modelName = GetEdmxResourceName(ctxType);
                string connectionString = $"metadata=res://*/{modelName}.csdl|res://*/{modelName}.ssdl|res://*/{modelName}.msl;provider=System.Data.SqlClient;provider connection string=\"\"";
                return connectionString;
            }

            private DbContext CreateConnectionStringAndDbContextWithDefaultCtor(Type ctxType)
            {
                try
                {
                    RemoveReadOnlyFromConnectionStringsSettings();
                    ConfigurationManager.ConnectionStrings.Remove(ctxType.Name);
                }
                catch { }
                try
                {
                    ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings(ctxType.Name, DefaultConnectionString, "System.Data.SqlClient"));
                }
                catch (Exception ex) { _errors.AppendLine("Couldnt add connection string " + FlattenExceptionMessages(ex)); }

                return CreateDbContextWithDefaultCtor(ctxType);
            }

            private void TryCreateSetConnectionStringInConfigIfNotExist(Type ctxType)
            {
                try
                {
                    RemoveReadOnlyFromConnectionStringsSettings();
                    if (ConfigurationManager.ConnectionStrings[ctxType.Name] == null)
                    {
                        var connectionString = DefaultConnectionString;
                        if (IsEdmxBasedContext(ctxType))
                        {
                            connectionString = CreateEdmxConnectionStringFor(ctxType);
                        }
                        ConfigurationManager.ConnectionStrings.Add(new ConnectionStringSettings(ctxType.Name, DefaultConnectionString, "System.Data.SqlClient"));
                    }
                }
                catch (Exception ex) { _errors.AppendLine("Couldnt add connection string " + FlattenExceptionMessages(ex)); }

            }

            private static void RemoveReadOnlyFromConnectionStringsSettings()
            {
                typeof(ConfigurationElementCollection)
                    .GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(ConfigurationManager.ConnectionStrings, false);
            }

            private DbContext CreateDbContextWithDefaultCtor(Type ctxType)
            {
                try
                {
                    return (DbContext)Activator.CreateInstance(ctxType);
                }
                catch (Exception ex)
                {
                    _errors.AppendLine(FlattenExceptionMessages(ex));
                    return null;
                }
            }

            private DbContext CreateDbContextWithDefaultConnectionString(Type ctxType)
            {
                try
                {
                    return (DbContext)Activator.CreateInstance(ctxType, new[] { DefaultConnectionString });
                }
                catch (Exception ex)
                {
                    _errors.AppendLine(FlattenExceptionMessages(ex));
                    return null;
                }
            }

            public static MetadataWorkspace GetMetadataWorkspaceOf(Type ctxType)
            {
                string modelName = GetEdmxResourceName(ctxType);
                return new MetadataWorkspace(new[] { $"res://*/{modelName}.csdl", $"res://*/{modelName}.ssdl", $"res://*/{modelName}.msl" }, new[] { ctxType.Assembly });
            }

            private static string GetEdmxResourceName(Type contextType)
            {
                const string csdlSuffix = ".csdl";

                if (TryGetEdmxName(contextType, out var modelName))
                {
                    modelName = contextType.Assembly.GetManifestResourceNames().First(res =>
                    {
                        return res.EndsWith(modelName + csdlSuffix);
                    });
                    modelName = modelName.Substring(0, modelName.Length - csdlSuffix.Length);
                }

                return modelName;
            }

            public static DbContextMapping GetMappings(DbContext context)
            {
                MetadataWorkspace metadata = null;
                Type ctxType = context.GetType();
                if (IsEdmxBasedContext(ctxType))
                {
                    metadata = GetMetadataWorkspaceOf(ctxType);
                }
                else
                {
                    metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

                }

                // Get the part of the model that contains info about the actual CLR types
                var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

                // Get the entity type from the model that maps to the CLR type
                var entityTypes = metadata
                        .GetItems<EntityType>(DataSpace.OSpace);


                // Get the entity set that uses this entity type
                var entitySet = metadata
                    .GetItems<EntityContainer>(DataSpace.CSpace)
                    .Single()
                    .EntitySets;


                // Find the mapping between conceptual and storage model for this entity set
                var mappings = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                        .Single()
                        .EntitySetMappings;

                var mappingMetadata =
                from mapping in mappings
                from entityTypeMapping in mapping.EntityTypeMappings
                from fragment in entityTypeMapping.Fragments
                let schema = fragment.StoreEntitySet.Schema
                let tableName = (string)fragment.StoreEntitySet.MetadataProperties["Table"].Value ?? fragment.StoreEntitySet.Name
                from propMapping in fragment.PropertyMappings.OfType<ScalarPropertyMapping>()
                select new { EntityName = entityTypeMapping.EntityType?.Name, EntityFullName = entityTypeMapping.EntityType?.FullName, Schema = schema, TableName = tableName, propName = propMapping.Property.Name, ColumnName = propMapping.Column.Name };

                var dbContextMapping = new DbContextMapping() { DbContextFullName = context.GetType().FullName, DbContextName = context.GetType().Name, MappingExtractionSucceeded = true };
                foreach (var mapping in mappingMetadata.GroupBy(x => new { x.EntityFullName, x.EntityName, x.Schema, x.TableName }))
                {
                    dbContextMapping.Entities.Add(
                        new EntityMapping
                        {
                            EntityName = mapping.Key.EntityName,
                            EntityFullName = mapping.Key.EntityFullName,
                            Schema = mapping.Key.Schema,
                            TableName = mapping.Key.TableName,
                            Properties = mapping.Select(x => new EntityPropertyMapping { PropertyName = x.propName, ColumnName = x.ColumnName }).ToList()
                        });
                }

                ExtractDbFunctions(metadata, dbContextMapping);

                return dbContextMapping;
            }

            private static void ExtractDbFunctions(MetadataWorkspace metadata, DbContextMapping dbContextMapping)
            {
                foreach (var function in metadata.GetItems<EdmFunction>(DataSpace.SSpace).Where(x => !x.NamespaceName.StartsWith("SqlServer")))
                {
                    dbContextMapping.DbFunctions.Add(new DbFunctionMapping { Name = function.Name, Schema = function.Schema, MappedMethodFullName = "" });
                }
            }

            private static bool IsEdmxBasedContext(Type ctxType)
            {
                return TryGetEdmxName(ctxType, out var _);
            }

            private static bool TryGetEdmxName(Type ctxType, out string edmxName)
            {
                var edmxNameField = ctxType.GetField("EdmxName", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (edmxNameField != null)
                {
                    edmxName = edmxNameField.GetRawConstantValue().ToString();
                    return true;
                }

                var edmxResourceName =
                    ctxType.Assembly.GetManifestResourceNames()
                        .Where(resource => resource.EndsWith(".msl"))
                        .FirstOrDefault(resource =>
                        {

                            using (Stream stream = ctxType.Assembly.GetManifestResourceStream(resource))
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                return reader.ReadToEnd().Contains($"CdmEntityContainer=\"{ctxType.Name}\"");
                            }
                        })
                        ?.Replace(".msl", "");

                if (edmxResourceName != null)
                {
                    edmxName = edmxResourceName;
                    return true;
                }

                edmxName = "";
                return false;
            }
        }
    }
}

#endif