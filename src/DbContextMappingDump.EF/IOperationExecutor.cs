using DbContextMappingDump.Infra.DataContracts;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DbContextMappingDump
{
    internal interface IOperationExecutor : IDisposable
    {
        DbContextMappings GetContextMappings();
    }
}
