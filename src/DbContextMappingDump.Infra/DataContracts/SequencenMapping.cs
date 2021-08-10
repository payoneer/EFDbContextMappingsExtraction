using System;

namespace DbContextMappingDump.Infra.DataContracts
{
    [Serializable]

    public class SequencenMapping
    {
        public string Schema { get; set; }
        public string Name { get; internal set; }
    }
}