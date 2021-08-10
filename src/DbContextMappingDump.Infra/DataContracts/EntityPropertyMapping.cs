using System;

namespace DbContextMappingDump.Infra.DataContracts
{
    [Serializable]
    public class EntityPropertyMapping
    {
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
    }
}