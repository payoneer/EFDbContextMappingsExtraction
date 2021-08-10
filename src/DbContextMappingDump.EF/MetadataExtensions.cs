using System;
using System.Reflection;
using ReflectionMagic;

namespace DbContextMappingDump
{
    /// <summary>
    /// Based on Cédric Luthi (0xced) code from https://github.com/dotnet/efcore/issues/18256
    /// </summary>
    class MetadataExtensions
    {
        private readonly Assembly EfCoreRelationalAssembly;
        // EF Core 2
        private dynamic RelationalMetadataExtensions => EfCoreRelationalAssembly?.GetType("Microsoft.EntityFrameworkCore.RelationalMetadataExtensions")?.AsDynamicType();
        // EF Core 3
        private dynamic RelationalEntityTypeExtensions => EfCoreRelationalAssembly?.GetType("Microsoft.EntityFrameworkCore.RelationalEntityTypeExtensions")?.AsDynamicType();
        private dynamic RelationalPropertyExtensions => EfCoreRelationalAssembly?.GetType("Microsoft.EntityFrameworkCore.RelationalPropertyExtensions")?.AsDynamicType();

        public MetadataExtensions(Assembly efCoreRelationalAssembly)
        {
            EfCoreRelationalAssembly = efCoreRelationalAssembly;
        }
        public string GetSchema(dynamic entityType)
        {
            if (RelationalEntityTypeExtensions != null)
                return RelationalEntityTypeExtensions.GetSchema(entityType);
            if (RelationalMetadataExtensions != null)
                return RelationalMetadataExtensions.Relational(entityType).Schema;
            throw NotSupportedException();
        }

        public string GetTableName(dynamic entityType)
        {
            if (RelationalEntityTypeExtensions != null)
                return RelationalEntityTypeExtensions.GetTableName(entityType);
            if (RelationalMetadataExtensions != null)
                return RelationalMetadataExtensions.Relational(entityType).TableName;
            throw NotSupportedException();
        }

        public string GetColumnName(dynamic property)
        {
            if (RelationalPropertyExtensions != null)
                return RelationalPropertyExtensions.GetColumnName(property);
            if (RelationalMetadataExtensions != null)
                return RelationalMetadataExtensions.Relational(property).ColumnName;
            throw NotSupportedException();
        }

        private Exception NotSupportedException()
        {
            if (EfCoreRelationalAssembly == null)
                throw new InvalidOperationException($"The 'Microsoft.EntityFrameworkCore.Relational' assembly was not found as a referenced assembly.");
            return new NotSupportedException($"Found neither 'Microsoft.EntityFrameworkCore.RelationalMetadataExtensions' (expected in EF Core 2) nor 'Microsoft.EntityFrameworkCore.RelationalEntityTypeExtensions' (expected in EF Core 3). Did Microsoft introduce a breaking change in {EfCoreRelationalAssembly.GetName()} ?");
        }
    }
}
