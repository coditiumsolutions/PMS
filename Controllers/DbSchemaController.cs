using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace PMS.Web.Controllers;

public class DbSchemaController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public DbSchemaController(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    [HttpGet]
    public IActionResult GenerateSchema()
    {
        try
        {
            var schemaReport = GenerateSchemaReport();
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "db.txt");
            System.IO.File.WriteAllText(outputPath, schemaReport);
            
            return Json(new { success = true, message = $"Schema report generated successfully at: {outputPath}" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    private string GenerateSchemaReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("DATABASE SCHEMA REPORT");
        sb.AppendLine("======================");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Get all user tables
        var tablesQuery = @"
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            AND TABLE_NAME NOT LIKE 'sys%'
            AND TABLE_NAME NOT LIKE '__%'
            ORDER BY TABLE_NAME";

        var tables = new List<(string Schema, string Name)>();
        using (var command = new SqlCommand(tablesQuery, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                tables.Add((reader["TABLE_SCHEMA"].ToString()!, reader["TABLE_NAME"].ToString()!));
            }
        }

        // For each table, get columns
        foreach (var (schema, tableName) in tables)
        {
            sb.AppendLine($"TABLE: {tableName}");
            sb.AppendLine(new string('-', tableName.Length + 7));

            var columnsQuery = @"
                SELECT 
                    COLUMN_NAME,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    IS_NULLABLE,
                    COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

            using var columnsCommand = new SqlCommand(columnsQuery, connection);
            columnsCommand.Parameters.AddWithValue("@Schema", schema);
            columnsCommand.Parameters.AddWithValue("@TableName", tableName);

            using var columnsReader = columnsCommand.ExecuteReader();
            while (columnsReader.Read())
            {
                var columnName = columnsReader["COLUMN_NAME"].ToString()!;
                var dataType = columnsReader["DATA_TYPE"].ToString()!;
                var maxLength = columnsReader["CHARACTER_MAXIMUM_LENGTH"] as int?;
                var isNullable = columnsReader["IS_NULLABLE"].ToString() == "YES";
                var defaultValue = columnsReader["COLUMN_DEFAULT"]?.ToString();

                var typeDescription = dataType.ToLower();
                if (maxLength.HasValue && maxLength.Value > 0)
                {
                    typeDescription += $"({maxLength.Value})";
                }
                else if (dataType.ToLower() == "nvarchar" || dataType.ToLower() == "varchar")
                {
                    typeDescription += "(max)";
                }

                if (isNullable)
                {
                    typeDescription += ", nullable";
                }

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    typeDescription += $", default: {defaultValue}";
                }

                sb.AppendLine($"- {columnName} ({typeDescription})");
            }
        }

        return sb.ToString();
    }
}
