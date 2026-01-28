using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using System.Data;
using System.Text;

namespace PMS.Web.Services;

public class SQLQueryService
{
    private readonly PMSDbContext _context;
    private readonly string _connectionString;

    public SQLQueryService(PMSDbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<string> GetDatabaseSchemaAsync()
    {
        var schema = new StringBuilder();
        schema.AppendLine("DATABASE SCHEMA:");
        schema.AppendLine("================");
        schema.AppendLine();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get all tables
            var tablesQuery = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

            using var command = new SqlCommand(tablesQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            await reader.CloseAsync();

            // Get columns for each table
            foreach (var tableName in tables)
            {
                schema.AppendLine($"TABLE: {tableName}");
                schema.AppendLine(new string('-', tableName.Length + 7));

                var columnsQuery = @"
                    SELECT 
                        COLUMN_NAME,
                        DATA_TYPE,
                        IS_NULLABLE,
                        COLUMN_DEFAULT
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION";

                using var columnsCommand = new SqlCommand(columnsQuery, connection);
                columnsCommand.Parameters.AddWithValue("@TableName", tableName);
                using var columnsReader = await columnsCommand.ExecuteReaderAsync();

                while (await columnsReader.ReadAsync())
                {
                    var columnName = columnsReader.GetString(0);
                    var dataType = columnsReader.GetString(1);
                    var isNullable = columnsReader.GetString(2);
                    var defaultValue = columnsReader.IsDBNull(3) ? null : columnsReader.GetString(3);

                    schema.Append($"- {columnName} ({dataType}");
                    if (isNullable == "YES")
                        schema.Append(", nullable");
                    if (!string.IsNullOrEmpty(defaultValue))
                        schema.Append($", default: ({defaultValue})");
                    schema.AppendLine(")");
                }
                await columnsReader.CloseAsync();

                schema.AppendLine();
            }
        }
        catch (Exception ex)
        {
            schema.AppendLine($"Error retrieving schema: {ex.Message}");
        }

        return schema.ToString();
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sqlQuery)
    {
        var result = new QueryResult();

        try
        {
            // Strong SQL injection protection - only allow SELECT statements
            var trimmedQuery = sqlQuery.Trim();
            var upperQuery = trimmedQuery.ToUpperInvariant();
            
            // Check if query starts with SELECT
            if (!upperQuery.StartsWith("SELECT"))
            {
                result.Error = "Only SELECT queries are allowed. INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, and other data modification statements are forbidden.";
                return result;
            }
            
            // Additional security checks - block dangerous keywords even if they appear after SELECT
            var dangerousKeywords = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "EXEC", "EXECUTE" };
            foreach (var keyword in dangerousKeywords)
            {
                if (upperQuery.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    result.Error = $"Security violation: '{keyword}' is not allowed. Only SELECT queries are permitted.";
                    return result;
                }
            }
            
            // Block SQL injection patterns: comments and stored procedure calls
            if (upperQuery.Contains("--") || upperQuery.Contains("/*") || upperQuery.Contains("*/") ||
                upperQuery.Contains("SP_") || upperQuery.Contains("XP_"))
            {
                result.Error = "Security violation: SQL injection patterns detected. Only SELECT queries are permitted.";
                return result;
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(sqlQuery, connection);
            command.CommandTimeout = 30;

            using var reader = await command.ExecuteReaderAsync();

            // Get column names
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }
            result.Columns = columns;

            // Get rows
            var rows = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value ?? DBNull.Value;
                }
                rows.Add(row);
            }

            result.Rows = rows;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Error = $"Error executing query: {ex.Message}";
            result.Success = false;
        }

        return result;
    }
}

public class QueryResult
{
    public bool Success { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object>> Rows { get; set; } = new();
    public string? Error { get; set; }
}
