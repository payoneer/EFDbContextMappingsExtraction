using DbContextMappingDump.Infra.DataContracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DbContextMappingDump.Commands
{
    // ReSharper disable once ArrangeTypeModifiers
    internal partial class DbContextMappingsCommand
    {
        protected override int Execute(string[] args)
        {
            var result = CreateExecutor(args).GetContextMappings();

            if (_json.HasValue())
            {
                Reporter.NoColor = false;
                Reporter.WriteDataLine("JsonResult:");

                ReportJsonResult(result);

                Reporter.WriteDataLine("JsonResult Done");
            }
            else
            {
                ReportResult(result);
            }

            return base.Execute(args);
        }

        private static void ReportJsonResult(DbContextMappings result)
        {
            Reporter.WriteDataLine("{");
            Reporter.WriteDataLine("  \"MappingExtractionSucceeded\": " + Json.Literal(result.MappingExtractionSucceeded) + ",");
            Reporter.WriteData("  \"ErrorDetails\": " + Json.Literal(result.ErrorDetails));
            if (result.DbContexts.Any())
            {
                Reporter.WriteDataLine(",");
                Reporter.WriteDataLine("  \"DbContexts\": ");
                WriteJsonArray(result.DbContexts, false, (dbContext, writeCommaAtEnd, indentation) => WriteDbContextMapping(dbContext, writeCommaAtEnd, indentation), 2);
            }

            Reporter.WriteDataLine("}");
        }

        private static void WriteDbContextMapping(DbContextMapping dbContext, bool writeCommaAtEnd, int indentation)
        {
            Reporter.WriteDataLine("{", indentation);
            Reporter.WriteDataLine("  \"DbContextName\": " + Json.Literal(dbContext.DbContextName) + ",", indentation);
            Reporter.WriteDataLine("  \"DbContextFullName\": " + Json.Literal(dbContext.DbContextFullName) + ",", indentation);
            Reporter.WriteDataLine("  \"MappingExtractionSucceeded\": " + Json.Literal(dbContext.MappingExtractionSucceeded) + ",", indentation);
            Reporter.WriteData("  \"ErrorDetails\": " + Json.Literal(dbContext.ErrorDetails), indentation);
            Reporter.WriteDataLine(",");
            Reporter.WriteDataLine("  \"Entities\": ", indentation);
            WriteJsonArray(dbContext.Entities, true, (e, writeCommaAtEnd, indent) => WriteEntityMapping(e, writeCommaAtEnd, indent), indentation + 2);
            Reporter.WriteDataLine("  \"Sequences\": ", indentation);
            WriteJsonArray(dbContext.Sequences, true, (e, writeCommaAtEnd, indent) => WriteSequenceMapping(e, writeCommaAtEnd, indent), indentation + 2);
            Reporter.WriteDataLine("  \"DbFunctions\": ", indentation);
            WriteJsonArray(dbContext.DbFunctions, false, (e, writeCommaAtEnd, indent) => WriteDbFunctionMapping(e, writeCommaAtEnd, indent), indentation + 2);
            Reporter.WriteData("}", indentation);
            if (writeCommaAtEnd)
            {
                Reporter.WriteDataLine(",", indentation);
            }
            else
            {
                Reporter.WriteDataLine("", indentation);
            }

        }

        static void WriteJsonArray<T>(IEnumerable<T> items, bool writeCommaAtEnd, Action<T, bool, int> itemWriter, int indent)
        {
            Reporter.WriteDataLine("[", indent);
            if (items.Any())
            {
                var lastItem = items.Last();
                foreach (var item in items.Take(items.Count() - 1))
                {
                    itemWriter(item, true, indent + 2);                    
                }

                itemWriter(lastItem, false, indent + 2);
            }
            Reporter.WriteData("]", indent);
            if (writeCommaAtEnd)
            {
                Reporter.WriteDataLine(",");
            }
            else
            {
                Reporter.WriteDataLine("");
            }

        }

        private static void WriteEntityMapping(EntityMapping entity, bool writeCommaAtEnd, int indent)
        {
            Reporter.WriteDataLine("{", indent);
            Reporter.WriteDataLine("  \"EntityName\": " + Json.Literal(entity.EntityName) + ",", indent);
            Reporter.WriteDataLine("  \"EntityFullName\": " + Json.Literal(entity.EntityFullName) + ",", indent);
            Reporter.WriteDataLine("  \"Schema\": " + Json.Literal(entity.Schema) + ",", indent);
            Reporter.WriteDataLine("  \"TableName\": " + Json.Literal(entity.TableName) + ",", indent);

            var properties = entity.Properties;
            Reporter.WriteDataLine("  \"Properties\": ", indent);
            WriteJsonArray(properties, false, (p, writeCommaAtEnd, indentation) => WritePropertyMapping(p, writeCommaAtEnd, indentation), indent + 2);
            Reporter.WriteData("}", indent);
            if (writeCommaAtEnd)
            {
                Reporter.WriteDataLine(",");
            }
            else
            {
                Reporter.WriteDataLine("");
            }
        }

        private static void WritePropertyMapping(EntityPropertyMapping prop, bool writeCommaAtEnd, int indentation)
        {
            Reporter.WriteDataLine("{", indentation);
            Reporter.WriteDataLine("  \"PropertyName\": " + Json.Literal(prop.PropertyName) + ",", indentation);
            Reporter.WriteDataLine("  \"ColumnName\": " + Json.Literal(prop.ColumnName) + "", indentation);
            Reporter.WriteData("}", indentation);
            if (writeCommaAtEnd)
            {
                Reporter.WriteDataLine(",");
            }
            else
            {
                Reporter.WriteDataLine("");
            }
        }

        private static void WriteDbFunctionMapping(DbFunctionMapping dbFunction, bool writeCommaAtEnd,  int indentation)
        {
            Reporter.WriteDataLine("{", indentation);
            Reporter.WriteDataLine("  \"Name\": " + Json.Literal(dbFunction.Name) + ",", indentation);
            Reporter.WriteDataLine("  \"Schema\": " + Json.Literal(dbFunction.Schema) + ",", indentation);
            Reporter.WriteDataLine("  \"MappedMethodFullName\": " + Json.Literal(dbFunction.MappedMethodFullName) + "", indentation);
            Reporter.WriteData("}", indentation);
            if (writeCommaAtEnd)
            {
                Reporter.WriteDataLine(",");
            }
            else 
            {
                Reporter.WriteDataLine("");
            }
        }

        private static void WriteSequenceMapping(SequencenMapping sequence, bool writeCommaAtEnd, int indentation)
        {
            Reporter.WriteDataLine("{", indentation);
            Reporter.WriteDataLine("  \"Name\": " + Json.Literal(sequence.Name) + ",", indentation);
            Reporter.WriteDataLine("  \"Schema\": " + Json.Literal(sequence.Schema) + "", indentation);
            Reporter.WriteData("}", indentation);
            if (writeCommaAtEnd)
            {
                Reporter.WriteDataLine(",");
            }
            else
            {
                Reporter.WriteDataLine("");
            }
        }

        private static void ReportResult(DbContextMappings result)
        {
            Reporter.WriteDataLine($"MappingExtractionSucceeded: {result.MappingExtractionSucceeded}");
            Reporter.WriteDataLine($"ErrorDetails: {result.ErrorDetails}");

            foreach (var dbContext in result.DbContexts)
            {
                Reporter.WriteDataLine($"-----------------------------{dbContext.DbContextName}-----------------------------------");
                Reporter.WriteDataLine($"DbContext: {dbContext.DbContextName}");
                Reporter.WriteDataLine($"DbContextFullName: {dbContext.DbContextFullName}");
                Reporter.WriteDataLine($"MappingExtractionSucceeded: {dbContext.MappingExtractionSucceeded}");
                Reporter.WriteDataLine($"ErrorDetails: {dbContext.ErrorDetails}");
                foreach (var mapping in dbContext.Entities)
                {
                    Reporter.WriteDataLine($"\tEntity: {mapping.EntityName} FullName:{mapping.EntityName} Schema: {mapping.Schema} Table: {mapping.TableName}");
                    foreach (var propMap in mapping.Properties)
                    {
                        Reporter.WriteDataLine($"\t\tProp: {propMap.PropertyName} Column: {propMap.ColumnName}");

                    }
                }
                Reporter.WriteDataLine("");
                Reporter.WriteDataLine("");
            }

        }
    }
}
