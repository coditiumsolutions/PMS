# PowerShell script to generate database schema report
param(
    [string]$ConnectionString = "Server=172.20.228.2;Database=PMS;User ID=sa;Password=Pakistan@786;TrustServerCertificate=True;",
    [string]$OutputFile = "db.txt"
)

$query = @"
SELECT 
    t.TABLE_SCHEMA,
    t.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    c.COLUMN_DEFAULT,
    c.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.TABLES t
INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
    AND t.TABLE_NAME NOT LIKE 'sys%'
ORDER BY t.TABLE_NAME, c.ORDINAL_POSITION
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $connection.Open()
    
    $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $dataset = New-Object System.Data.DataSet
    $adapter.Fill($dataset) | Out-Null
    
    $output = New-Object System.Text.StringBuilder
    $output.AppendLine("DATABASE SCHEMA REPORT") | Out-Null
    $output.AppendLine("======================") | Out-Null
    $output.AppendLine() | Out-Null
    $output.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')") | Out-Null
    $output.AppendLine() | Out-Null
    
    $currentTable = ""
    foreach ($row in $dataset.Tables[0].Rows) {
        $tableName = $row["TABLE_NAME"].ToString()
        $columnName = $row["COLUMN_NAME"].ToString()
        $dataType = $row["DATA_TYPE"].ToString()
        $maxLength = if ($row["CHARACTER_MAXIMUM_LENGTH"] -ne [DBNull]::Value) { $row["CHARACTER_MAXIMUM_LENGTH"] } else { $null }
        $isNullable = $row["IS_NULLABLE"].ToString() -eq "YES"
        $defaultValue = if ($row["COLUMN_DEFAULT"] -ne [DBNull]::Value) { $row["COLUMN_DEFAULT"].ToString() } else { $null }
        
        if ($currentTable -ne $tableName) {
            if ($currentTable -ne "") {
                $output.AppendLine() | Out-Null
            }
            $currentTable = $tableName
            $output.AppendLine("TABLE: $tableName") | Out-Null
            $output.AppendLine(("-" * ($tableName.Length + 7))) | Out-Null
        }
        
        $typeDescription = $dataType.ToLower()
        if ($maxLength -and $maxLength -gt 0) {
            $typeDescription += "($maxLength)"
        } elseif ($dataType.ToLower() -eq "nvarchar" -or $dataType.ToLower() -eq "varchar") {
            $typeDescription += "(max)"
        }
        
        if ($isNullable) {
            $typeDescription += ", nullable"
        }
        
        if ($defaultValue) {
            $typeDescription += ", default: $defaultValue"
        }
        
        $output.AppendLine("- $columnName ($typeDescription)") | Out-Null
    }
    
    $outputFile = Join-Path $PSScriptRoot "..\$OutputFile"
    [System.IO.File]::WriteAllText($outputFile, $output.ToString())
    
    Write-Host "Schema report generated successfully: $outputFile" -ForegroundColor Green
    
    $connection.Close()
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
