using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;

// Find appsettings.json in project root
var baseDir = AppContext.BaseDirectory;
var currentDir = Directory.GetCurrentDirectory();
var candidates = new[]
{
    Path.Combine(baseDir, "..", "..", "..", "..", "appsettings.json"),
    Path.Combine(currentDir, "appsettings.json"),
    Path.Combine(currentDir, "..", "appsettings.json"),
};
var appSettingsPath = candidates.FirstOrDefault(File.Exists);
if (string.IsNullOrEmpty(appSettingsPath))
{
    Console.WriteLine("appsettings.json not found.");
    return 1;
}

var json = await File.ReadAllTextAsync(appSettingsPath);
using var doc = JsonDocument.Parse(json);
var connSection = doc.RootElement.GetProperty("ConnectionStrings");
var connectionString = connSection.GetProperty("DefaultConnection").GetString();
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("DefaultConnection not found.");
    return 1;
}

var outPath = Path.Combine(Path.GetDirectoryName(appSettingsPath)!, "db.txt");
var tables = new Dictionary<string, List<ColumnInfo>>();

const string columnsSql = """
    SELECT 
        c.TABLE_SCHEMA,
        c.TABLE_NAME,
        c.COLUMN_NAME,
        c.DATA_TYPE,
        c.CHARACTER_MAXIMUM_LENGTH,
        c.IS_NULLABLE,
        c.COLUMN_DEFAULT,
        c.ORDINAL_POSITION
    FROM INFORMATION_SCHEMA.COLUMNS c
    WHERE c.TABLE_CATALOG = DB_NAME()
    ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION
    """;

const string pkSql = """
    SELECT 
        tc.TABLE_NAME,
        ccu.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ccu 
        ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME AND tc.TABLE_SCHEMA = ccu.TABLE_SCHEMA
    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_CATALOG = DB_NAME()
    """;

var pkColumns = new HashSet<(string Table, string Column)>();

await using (var conn = new SqlConnection(connectionString))
{
    await conn.OpenAsync();

    await using (var cmd = new SqlCommand(columnsSql, conn))
    await using (var r = await cmd.ExecuteReaderAsync())
    {
        while (await r.ReadAsync())
        {
            var table = $"{r.GetString(0)}.{r.GetString(1)}";
            if (!tables.ContainsKey(table))
                tables[table] = new List<ColumnInfo>();
            tables[table].Add(new ColumnInfo(
                r.GetString(2),
                r.GetString(3),
                r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                r.GetString(5) == "YES",
                r.IsDBNull(6) ? null : r.GetString(6)?.Trim()
            ));
        }
    }

    await using (var cmd = new SqlCommand(pkSql, conn))
    await using (var r = await cmd.ExecuteReaderAsync())
    {
        while (await r.ReadAsync())
            pkColumns.Add((r.GetString(0), r.GetString(1)));
    }
}

var sb = new System.Text.StringBuilder();
sb.AppendLine("DATABASE SCHEMA REPORT");
sb.AppendLine("======================");
sb.AppendLine();
sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
sb.AppendLine();

foreach (var kv in tables.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
{
    var tableName = kv.Key;
    var cols = kv.Value;
    sb.AppendLine("TABLE: " + tableName);
    sb.AppendLine("----------------------------");
    var shortTableName = tableName.Contains('.') ? tableName.Split('.')[^1] : tableName;
    foreach (var c in cols)
    {
        var pk = pkColumns.Contains((shortTableName, c.Name)) ? ", PK" : "";
        var type = c.DataType;
        if (c.MaxLength.HasValue && (type == "varchar" || type == "nvarchar" || type == "char" || type == "nchar"))
            type += "(" + (c.MaxLength == -1 ? "max" : c.MaxLength.ToString()) + ")";
        var nullable = c.IsNullable ? ", nullable" : "";
        var def = string.IsNullOrEmpty(c.Default) ? "" : ", default: " + c.Default;
        sb.AppendLine("- " + c.Name + " (" + type + pk + nullable + def + ")");
    }
    sb.AppendLine();
}

await File.WriteAllTextAsync(outPath, sb.ToString());
Console.WriteLine("Updated: " + outPath);
return 0;

record ColumnInfo(string Name, string DataType, int? MaxLength, bool IsNullable, string? Default);
