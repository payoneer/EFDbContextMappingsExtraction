extern alias DbContextMappingDumpInfra;

using DbContextMappingDumpInfra::DbContextMappingDump.Infra.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Reflection;
using EFVersion = DbContextMappingDumpInfra::DbContextMappingDump.Infra.DataContracts.EFVersion;
using NETVersion = DbContextMappingDumpInfra::DbContextMappingDump.Infra.DataContracts.NETVersion;
namespace DbContextMappingDump.Main.Tests
{
    [TestClass]
    public class RootCommandTests
    {
        string ToolsLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "../../../../artifacts/bin/debug");

        [TestMethod]
        public void ExtractMappings_returns_mappings_of_netcore_with_efcore_dbcontext()
        {
            var rootCommand = new RootCommand(
                Path.Combine(ToolsLocation, "netcoreapp2.0"),
                Path.Combine(ToolsLocation, "net461"));
            var result =
                rootCommand.ExtractMappings(
                    dbContextAssemblyPath: typeof(NETCore_EFCore_OneDBContext.UniversityContext).Assembly.Location,
                    startupAssemblyPath: typeof(RootCommandTests).Assembly.Location,
                    eFVersion: EFVersion.EFCore,
                    netVersion: NETVersion.NETCore);

            var mappings = DeserializeDbContextMappings(result);

            Assert.AreEqual(1, mappings.DbContexts.Count);
            var dbContextMapping = mappings.DbContexts.First();
            Assert.AreEqual(nameof(NETCore_EFCore_OneDBContext.UniversityContext), dbContextMapping.DbContextName);
            Assert.AreEqual(nameof(NETCore_EFCore_OneDBContext.Student), dbContextMapping.Entities.Single().EntityName);
            Assert.AreEqual(NETCore_EFCore_OneDBContext.UniversityContext.StudentsTableName, dbContextMapping.Entities.Single().TableName);
            Assert.AreEqual(NETCore_EFCore_OneDBContext.UniversityContext.Schema, dbContextMapping.Entities.Single().Schema);
            Assert.AreEqual(2, dbContextMapping.Entities.Single().Properties.Count);
            Assert.AreEqual(NETCore_EFCore_OneDBContext.UniversityContext.StoredProcName, dbContextMapping.DbFunctions.Single().Name);

        }

        [TestMethod]
        public void ExtractMappings_returns_mappings_of_fullnet_with_efcore_dbcontext()
        {
            var dbContextAssembly =
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    @"../../../../tests/sampleprojects/FullNet_EFCore_OneDbContext\bin\Debug\FullNet_EFCore_OneDbContext.dll");
            var rootCommand = new RootCommand(
                Path.Combine(ToolsLocation, "netcoreapp2.0"),
                Path.Combine(ToolsLocation, "net461"));

            var result =
                rootCommand.ExtractMappings(
                    dbContextAssemblyPath: dbContextAssembly,
                    //startupAssemblyPath: typeof(RootCommandTests).Assembly.Location,
                    eFVersion: EFVersion.EFCore,
                    netVersion: NETVersion.NETFramework);
            var mappings = DeserializeDbContextMappings(result);

            Assert.AreEqual(1, mappings.DbContexts.Count);
            var dbContextMapping = mappings.DbContexts.First();
            Assert.AreEqual("UniversityContext", dbContextMapping.DbContextName);
            Assert.AreEqual("Student", dbContextMapping.Entities.Single().EntityName);
            Assert.AreEqual("Students", dbContextMapping.Entities.Single().TableName);
            Assert.AreEqual("SomeSchema", dbContextMapping.Entities.Single().Schema);
            Assert.AreEqual(2, dbContextMapping.Entities.Single().Properties.Count);

        }

       

        [TestMethod]
        public void ExtractMappings_returns_mappings_of_fullnet_with_ef6_dbcontext()
        {
            var dbContextAssembly =
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    @"../../../../tests/sampleprojects/FullNet_EF6_OneDbContext\bin\Debug\FullNet_EF6_OneDbContext.dll");
            var rootCommand = new RootCommand(
                Path.Combine(ToolsLocation, "netcoreapp2.0"),
                Path.Combine(ToolsLocation, "net461"));


            var result =
                rootCommand.ExtractMappings(
                    dbContextAssemblyPath: dbContextAssembly,
                    //startupAssemblyPath: typeof(RootCommandTests).Assembly.Location,
                    eFVersion: EFVersion.EF6,
                    netVersion: NETVersion.NETFramework);

            var mappings = DeserializeDbContextMappings(result);

            Assert.AreEqual(1, mappings.DbContexts.Count);
            var dbContextMapping = mappings.DbContexts.First();
            Assert.AreEqual("UniversityContext", dbContextMapping.DbContextName);
            Assert.AreEqual("Student", dbContextMapping.Entities.Single().EntityName);
            Assert.AreEqual("Students", dbContextMapping.Entities.Single().TableName);
            Assert.AreEqual("SomeSchema", dbContextMapping.Entities.Single().Schema);
            Assert.AreEqual(2, dbContextMapping.Entities.Single().Properties.Count);

        }

        private static DbContextMappings DeserializeDbContextMappings((int exitCode, string output) result)
        {
            var startOfContent = result.output.IndexOf(RootCommand.JsonResultHeader) + RootCommand.JsonResultHeader.Length;
            var endOfContent = result.output.IndexOf(RootCommand.JsonResultFooter);
            var jsonContent = result.output.Substring(startOfContent, endOfContent - startOfContent);
            var mappings = JsonConvert.DeserializeObject<DbContextMappings>(jsonContent);
            return mappings;
        }
    }
}
