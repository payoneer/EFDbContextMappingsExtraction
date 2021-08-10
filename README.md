# EFDbContextMappingsExtraction
A tool to extract the mappings of entities to DB objects from the EF DbContexts in an assembly.

The tool will list all the DbContext in an assembly and the mappings of each entity it track to DB tables, column, stored procedures, functions and sequences.
The tool works similarly to [EF Core tools](https://docs.microsoft.com/en-us/ef/core/cli/dotnet) and was based on it (some code was even copied), but unlike the EF tools, this tool use reflection to try and create complex DbContexts without requiring the [design-time services](https://docs.microsoft.com/en-us/ef/core/cli/services) 

# Usage

```
Usage: DbContextMappingDump dbcontext mappings [options]

Options:
  --json                                 Show JSON output
  -p|--project <PROJECT>                 The project to use. Defaults to the current working directory.
  -s|--startup-project <PROJECT>         The startup project to use. Defaults to the current working directory.
  --context-assembly <assembly>          path to the assembly containing the dbcontext
  --startup-assembly <assembly>          path to the assembly containing the startup
  --framework <FRAMEWORK>                .NETCoreApp | .NETFramework
  --ef-version <EFVersion>               EF6 | EFCore
  --configuration <CONFIGURATION>
  --runtime <RUNTIME_IDENTIFIER>
  --msbuildprojectextensionspath <PATH>
  --no-build
  -h|--help                              Show help information
  -v|--verbose
  -d|--debug                             allow attaching to process for debugging purposes
  --no-color
```


# Example
`DbContextMappingDump dbcontext mappings --context-assembly FullNet_EF6_OneDbContext.dll --ef-version EF6 --framework .NETFramework --json`
```
JsonResult:
{
  "MappingExtractionSucceeded": true,
  "ErrorDetails": null,
  "DbContexts":
  [
    {
      "DbContextName": "UniversityContext",
      "DbContextFullName": "FullNet_EF6_OneDbContext.UniversityContext",
      "MappingExtractionSucceeded": true,
      "ErrorDetails": null,
      "Entities":
      [
        {
          "EntityName": "Student",
          "EntityFullName": "FullNet_EF6_OneDbContext.Student",
          "Schema": "SomeSchema",
          "TableName": "Students",
          "Properties":
          [
            {
              "PropertyName": "Id",
              "ColumnName": "Id"
            },
            {
              "PropertyName": "Name",
              "ColumnName": "Name"
            }
          ]
        }
      ],
      "Sequences":
      [
      ],
      "DbFunctions":
      [
      ]
    }
  ]
}
JsonResult Done
```

`DbContextMappingDump dbcontext mappings --context-assembly NETCore_EFCore_OneDBContext.dll --startup-assembly DbContextMappingDump.Main.Tests.dll --ef-version EFCore --framework .NETCoreApp --json`
```
JsonResult:
{
  "MappingExtractionSucceeded": true,
  "ErrorDetails": null,
  "DbContexts":
  [
    {
      "DbContextName": "UniversityContext",
      "DbContextFullName": "NETCore_EFCore_OneDBContext.UniversityContext",
      "MappingExtractionSucceeded": true,
      "ErrorDetails": null,
      "Entities":
      [
        {
          "EntityName": "Student",
          "EntityFullName": "NETCore_EFCore_OneDBContext.Student",
          "Schema": "SomeSchema",
          "TableName": "Students",
          "Properties":
          [
            {
              "PropertyName": "Id",
              "ColumnName": "Id"
            },
            {
              "PropertyName": "Name",
              "ColumnName": "Name"
            }
          ]
        }
      ],
      "Sequences":
      [
        {
          "Name": "SEQ_Students",
          "Schema": ""
        }
      ],
      "DbFunctions":
      [
        {
          "Name": "SP_CoursesForStudent",
          "Schema": "dbo",
          "MappedMethodFullName": "NETCore_EFCore_OneDBContext.UniversityContext.CoursesCountForStudent"
        }
      ]
    }
  ]
}
JsonResult Done
```