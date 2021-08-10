using System;
using System.Collections.Generic;

namespace DbContextMappingDump.Infra.DataContracts
{
    [Serializable]
    public class EntityMapping
    {
        public string TableName { get; set; }
        public string EntityName { get; set; }
        public string EntityFullName { get; set; }
        public string Schema { get; set; }

        public List<EntityPropertyMapping> Properties { get; set; } = new List<EntityPropertyMapping>();
    }
}
