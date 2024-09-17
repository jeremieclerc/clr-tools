using Microsoft.SqlServer.Server;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;


class GenerateBinary
{
    static async Task Main(string[] args)
    {
        // Define the arguments and options
        var inputFolderArgument = new Argument<string>("inputFolder", "The folder containing the .dll file");
        var outputFolderArgument = new Argument<string>("outputFolder", "The folder to output the generated SQL file");
        var dllNameArgument = new Argument<string>("dllName", description: "The name of the DLL file (e.g. AssemblyHttp)");
        var functionNameOption = new Option<string>("--functionName", description: "The name of the SQL CLR function to be created. Defaults to the name of the DLL prefixed by 'Function'.");

        // Define the root command with arguments and options
        var rootCommand = new RootCommand
        {
            inputFolderArgument,
            outputFolderArgument,
            dllNameArgument,
            functionNameOption
        };

        rootCommand.Description = "CLI Tool to generate SQL file for CLR installation on SQL Server";

        // Handler for the command
        rootCommand.SetHandler((InvocationContext context) =>
        {
            // Get the parsed arguments
            var inputFolder = context.ParseResult.GetValueForArgument(inputFolderArgument);
            var outputFolder = context.ParseResult.GetValueForArgument(outputFolderArgument);
            var dllName = context.ParseResult.GetValueForArgument(dllNameArgument);
            var functionName = context.ParseResult.GetValueForOption(functionNameOption) ?? $"Function{dllName}";

            // Execute the handler logic
            GenerateSqlFileAsync(inputFolder, outputFolder, dllName, functionName).Wait();
        });

        // Invoke the command handler
        await rootCommand.InvokeAsync(args);
    }

    static async Task GenerateSqlFileAsync(string inputFolder, string outputFolder, string dllName, string functionName)
    {
        try
        {
            string dllPath = Path.Combine(inputFolder, dllName + ".dll");
            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"Error: DLL file not found at {dllPath}");
                return;
            }

            // Generate binary content from DLL
            StringBuilder builder = new StringBuilder();
            builder.Append("0x");

            using (FileStream stream = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int currentByte = stream.ReadByte();
                while (currentByte > -1)
                {
                    builder.Append(currentByte.ToString("X2", CultureInfo.InvariantCulture));
                    currentByte = stream.ReadByte();
                }
            }
            string generatedBinary = builder.ToString();

            // Load the assembly using reflection
            Assembly assembly = Assembly.LoadFrom(dllPath);

            // Build the SQL script
            StringBuilder scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine($@"
USE META;
IF (SELECT TOP 1 value FROM sys.configurations WHERE name = 'clr enabled') != 1
BEGIN
    EXEC sp_configure 'clr enabled', 1
    RECONFIGURE
END
DECLARE @sql nvarchar(MAX), @hash varbinary(64)
DECLARE @clrBinary varbinary(MAX) = {generatedBinary}

SET @hash = (SELECT TOP 1 hash FROM sys.trusted_assemblies WHERE description = '{dllName}')
IF @hash IS NOT NULL EXEC sp_drop_trusted_assembly @hash
DROP FUNCTION IF EXISTS dbo.{functionName}
DROP ASSEMBLY IF EXISTS {dllName}

SET @hash = HASHBYTES('SHA2_512', @clrBinary)
EXEC sp_add_trusted_assembly @hash, N'{dllName}'
CREATE ASSEMBLY {dllName} AUTHORIZATION [dbo] FROM @clrBinary WITH PERMISSION_SET = UNSAFE;
            ");

            // Iterate over the types and methods to find SQL CLR functions
            foreach (Type type in assembly.GetExportedTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    // Check if method is marked as SqlFunction
                    var sqlFunctionAttr = method.GetCustomAttribute<SqlFunctionAttribute>();

                    if (sqlFunctionAttr != null)
                    {
                        // Generate SQL script for the function or procedure
                        string sqlObjectName = method.Name;
                        string sqlParameters = GenerateSqlParameters(method);
                        string sqlReturnType = GenerateSqlReturnType(method);

                        scriptBuilder.AppendLine($@"
EXEC ('CREATE FUNCTION dbo.{sqlObjectName}({sqlParameters})
    RETURNS {sqlReturnType} AS EXTERNAL NAME [{dllName}].[{type.FullName}].[{method.Name}]');
                        ");
                    }
                }
            }

            // Write the SQL script to a file
            string outputPath = Path.Combine(outputFolder, $"{dllName}_{DateTime.Today:yyyy-MM-dd}.sql");
            await File.WriteAllTextAsync(outputPath, scriptBuilder.ToString());

            Console.WriteLine($"SQL file generated successfully at {outputPath}");

            // Optionally open the folder containing the output file
            Process.Start("explorer.exe", outputFolder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string GenerateSqlParameters(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        StringBuilder paramBuilder = new StringBuilder();
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo param = parameters[i];
            string sqlType = MapToSqlDbType(param.ParameterType, param);
            paramBuilder.Append($"@{param.Name} {sqlType}");
            if (i < parameters.Length - 1)
                paramBuilder.Append(", ");
        }
        return paramBuilder.ToString();
    }

    static string GenerateSqlReturnType(MethodInfo method)
    {
        Type returnType = method.ReturnType;

        if (returnType == typeof(void))
        {
            return "VOID";
        }
        else if (typeof(IEnumerable).IsAssignableFrom(returnType) && returnType != typeof(string))
        {
            // Check if the method has SqlFunctionAttribute with TableDefinition
            var sqlFunctionAttr = method.GetCustomAttribute<SqlFunctionAttribute>();
            if (sqlFunctionAttr != null && !string.IsNullOrEmpty(sqlFunctionAttr.TableDefinition))
            {
                return "TABLE (" + sqlFunctionAttr.TableDefinition + ")";
            }

            // Else, try to get the element type
            Type elementType = GetElementType(returnType);
            if (elementType == null)
            {
                throw new NotSupportedException("Cannot determine the element type of the collection.");
            }

            // Get properties of the element type
            PropertyInfo[] properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (properties.Length == 0)
            {
                throw new NotSupportedException("The return type's element type has no public properties.");
            }

            // Build the table definition
            StringBuilder tableBuilder = new StringBuilder();
            tableBuilder.Append("TABLE (");

            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                string sqlType = MapToSqlDbType(prop.PropertyType, prop);
                tableBuilder.Append($"{prop.Name} {sqlType}");
                if (i < properties.Length - 1)
                    tableBuilder.Append(", ");
            }

            tableBuilder.Append(")");
            return tableBuilder.ToString();
        }
        else
        {
            string sqlType = MapToSqlDbType(returnType, (PropertyInfo)null);
            return sqlType;
        }
    }

    static Type GetElementType(Type returnType)
    {
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return returnType.GetGenericArguments()[0];
        }
        else if (returnType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            return returnType.GetInterfaces().First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)).GetGenericArguments()[0];
        }
        else if (returnType.IsArray)
        {
            return returnType.GetElementType();
        }
        return null;
    }

    static string MapToSqlDbType(Type type, ParameterInfo parameterInfo)
    {
        // Check for SqlFacetAttribute on the parameter, if any
        var sqlFacetAttr = parameterInfo.GetCustomAttribute<SqlFacetAttribute>();
        return MapToSqlDbTypeCore(type, sqlFacetAttr);
    }

    static string MapToSqlDbType(Type type, PropertyInfo propertyInfo)
    {
        // Check for SqlFacetAttribute on the property, if any
        var sqlFacetAttr = propertyInfo.GetCustomAttribute<SqlFacetAttribute>();
        return MapToSqlDbTypeCore(type, sqlFacetAttr);
    }


    static string MapToSqlDbTypeCore(Type type, SqlFacetAttribute sqlFacetAttr)
    {
        int? maxSize = null;
        int? precision = null;
        int? scale = null;

        if (sqlFacetAttr != null)
        {
            if (type == typeof(string) || type == typeof(byte[]))
            {
                if (sqlFacetAttr.MaxSize > 0 && sqlFacetAttr.MaxSize <= 4000)
                {
                    maxSize = sqlFacetAttr.MaxSize;
                }
                else
                {
                    maxSize = null; // default to MAX
                }
            }
            precision = sqlFacetAttr.Precision;
            scale = sqlFacetAttr.Scale;
        }

        // Map .NET types to SQL Server types
        if (type == typeof(string))
        {
            if (maxSize.HasValue)
            {
                return $"NVARCHAR({maxSize.Value})";
            }
            else
            {
                return "NVARCHAR(MAX)";
            }
        }
        else if (type == typeof(int))
            return "INT";
        else if (type == typeof(short))
            return "SMALLINT";
        else if (type == typeof(long))
            return "BIGINT";
        else if (type == typeof(bool))
            return "BIT";
        else if (type == typeof(DateTime))
            return "DATETIME";
        else if (type == typeof(decimal))
        {
            if (precision.HasValue && scale.HasValue)
            {
                return $"DECIMAL({precision.Value},{scale.Value})";
            }
            else
            {
                return "DECIMAL(18,2)";
            }
        }
        else if (type == typeof(double))
            return "FLOAT";
        else if (type == typeof(byte[]))
        {
            if (maxSize.HasValue)
            {
                return $"VARBINARY({maxSize.Value})";
            }
            else
            {
                return "VARBINARY(MAX)";
            }
        }
        else
            return "NVARCHAR(MAX)"; // Default to NVARCHAR(MAX) for unknown types
    }
}
