using System;

namespace DbContextMappingDump.Infra.DataContracts
{
    [Serializable]

    public class DbFunctionMapping
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public string MappedMethodFullName { get; set; }
    }
}