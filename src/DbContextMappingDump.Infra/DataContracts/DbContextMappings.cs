using System;
using System.Collections.Generic;

namespace DbContextMappingDump.Infra.DataContracts
{
    [Serializable]
    public class DbContextMappings
    {
        public bool MappingExtractionSucceeded { get; set; } = true;
        public string ErrorDetails { get; set; }
        public List<DbContextMapping> DbContexts { get; set; } = new List<DbContextMapping>();
    }
    [Serializable]
    public class DbContextMapping
    {
        public string DbContextName { get; set; }
        public string DbContextFullName { get; set; }
        public bool MappingExtractionSucceeded { get; set; } = true;
        public string ErrorDetails { get; set; }
        public List<EntityMapping> Entities { get; set; } = new List<EntityMapping>();
        public List<DbFunctionMapping> DbFunctions { get; set; } = new List<DbFunctionMapping>();
        public List<SequencenMapping> Sequences { get; set; } = new List<SequencenMapping>();
    }
}
