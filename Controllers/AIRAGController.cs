using Microsoft.AspNetCore.Mvc;
using PMS.Web.Services;
using PMS.Web.ViewModels;
using System.Text.Json;
using System.IO;

namespace PMS.Web.Controllers;

public class AIRAGController : Controller
{
    private readonly GroqAIService _groqService;
    private readonly SQLQueryService _sqlService;
    private readonly ILogger<AIRAGController> _logger;
    private readonly IConfiguration _configuration;

    public AIRAGController(
        GroqAIService groqService,
        SQLQueryService sqlService,
        ILogger<AIRAGController> logger,
        IConfiguration configuration)
    {
        _groqService = groqService;
        _sqlService = sqlService;
        _logger = logger;
        _configuration = configuration;
    }

    // GET: AIRAG
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveModule = "AIRAG";
        return View();
    }

    // POST: AIRAG/Chat
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequestViewModel request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, error = "Message cannot be empty." });
            }

            // Load configuration limits
            var maxSchemaSize = int.Parse(_configuration["Groq:MaxSchemaSize"] ?? "50000");
            
            // Load database schema from db.txt file
            var dbSchemaPath = Path.Combine(Directory.GetCurrentDirectory(), "db.txt");
            var dbSchema = "DATABASE SCHEMA NOT FOUND";
            if (System.IO.File.Exists(dbSchemaPath))
            {
                var fullSchema = await System.IO.File.ReadAllTextAsync(dbSchemaPath);
                // Truncate schema if too large (configurable limit)
                dbSchema = fullSchema.Length > maxSchemaSize 
                    ? fullSchema.Substring(0, maxSchemaSize) + $"\n\n[Schema truncated for size - showing first {maxSchemaSize} characters of {fullSchema.Length} total...]"
                    : fullSchema;
            }
            else
            {
                // Fallback to dynamic schema if file doesn't exist
                var dynamicSchema = await _sqlService.GetDatabaseSchemaAsync();
                dbSchema = dynamicSchema.Length > maxSchemaSize 
                    ? dynamicSchema.Substring(0, maxSchemaSize) + $"\n\n[Schema truncated for size - showing first {maxSchemaSize} characters of {dynamicSchema.Length} total...]"
                    : dynamicSchema;
            }

            // Build system prompt with database schema
            var systemPrompt = $@"You are an AI assistant that helps users query a Property Management System (PMS) database.

CRITICAL SECURITY RULES:
- YOU MUST ONLY GENERATE SELECT QUERIES
- NEVER generate INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, or any data modification statements
- If a user asks to modify data, politely explain that you can only read data, not modify it
- Only SELECT statements are allowed - this is enforced at multiple security layers

DATABASE TYPE: Microsoft SQL Server (MSSQL)
CRITICAL SQL SERVER DATE FUNCTIONS (USE THESE - DO NOT USE EXTRACT):
- Current date/time: GETDATE() or CURRENT_TIMESTAMP
- Extract year: YEAR(date_column) - NOT EXTRACT(YEAR FROM date_column)
- Extract month: MONTH(date_column) - NOT EXTRACT(MONTH FROM date_column)
- Extract day: DAY(date_column) - NOT EXTRACT(DAY FROM date_column)
- Date part: DATEPART(year, date_column), DATEPART(month, date_column), etc.
- Date arithmetic: DATEADD(day, 1, date_column), DATEADD(month, 1, date_column)
- Date difference: DATEDIFF(day, date1, date2)
- FORBIDDEN: EXTRACT() function does NOT exist in SQL Server - NEVER use it

CRITICAL: STRING DATE COLUMNS (VARCHAR):
Some date columns are stored as VARCHAR (string) in the database, NOT as date/datetime types:
- Payments.CreatedOn is VARCHAR(60) - stored as string
- Payments.PaidDate is VARCHAR(60) - stored as string
- Payments.DSDate, DDDate, ChequeDate are VARCHAR(60) - stored as string

FOR STRING DATE COLUMNS, USE ONE OF THESE METHODS:
1. TRY_CONVERT with proper format (SAFEST):
   - TRY_CONVERT(DATETIME, CreatedOn, 120) - for 'YYYY-MM-DD HH:MM:SS' format
   - TRY_CONVERT(DATETIME, CreatedOn, 101) - for 'MM/DD/YYYY' format
   - TRY_CONVERT(DATETIME, CreatedOn, 103) - for 'DD/MM/YYYY' format
   - TRY_CONVERT(DATE, CreatedOn, 120) - for date only

2. CAST with ISDATE check:
   - WHERE ISDATE(CreatedOn) = 1 AND CAST(CreatedOn AS DATETIME) >= DATEADD(day, -30, GETDATE())

3. String comparison (if dates are stored in 'YYYY-MM-DD' format):
   - WHERE CreatedOn LIKE '2025-01%' - for January 2025
   - WHERE CreatedOn >= '2025-01-01' AND CreatedOn < '2025-02-01' - for January 2025

DATE QUERY EXAMPLES FOR SQL SERVER:
- For datetime columns: WHERE YEAR(CreationDate) = YEAR(GETDATE()) AND MONTH(CreationDate) = MONTH(GETDATE())
- For string date columns (Payments.CreatedOn): WHERE YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE()) AND MONTH(TRY_CONVERT(DATETIME, CreatedOn, 120)) = MONTH(GETDATE())
- This year (datetime): WHERE YEAR(CreationDate) = YEAR(GETDATE())
- This year (string date): WHERE YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE())
- Last 30 days (datetime): WHERE CreationDate >= DATEADD(day, -30, GETDATE())
- Last 30 days (string date): WHERE TRY_CONVERT(DATETIME, CreatedOn, 120) >= DATEADD(day, -30, GETDATE())
- Date range (datetime): WHERE CreationDate BETWEEN '2025-01-01' AND '2025-01-31'
- Date range (string date): WHERE TRY_CONVERT(DATETIME, CreatedOn, 120) BETWEEN '2025-01-01' AND '2025-01-31'

DATABASE SCHEMA (EXACT TABLE AND COLUMN NAMES - USE THESE EXACTLY):
{dbSchema}

CRITICAL TABLE NAME RULES:
1. BEFORE generating any SQL query, you MUST look at the DATABASE SCHEMA above and find the EXACT table name.
2. NEVER invent table names. NEVER use plural forms (like ""inventories"", ""plots"", ""customers""). NEVER use lowercase versions.
3. When users say ""inventory"", ""inventories"", ""plots"", ""plot"", ""properties"", ""units"", or ""items"", you MUST use the EXACT table name: InventoryDetail (with capital I and D)
4. When users say ""customers"", you MUST use: Customers (capital C)
5. When users say ""payments"", you MUST use: Payments (capital P)
6. When users say ""projects"", you MUST use: Projects (capital P)
7. When users say ""transfers"", you MUST use: Transfer (capital T, singular)
8. When users say ""schedules"", ""plans"", you MUST use: PaymentPlan or paymentplanchild (exact case as shown in schema)

FORBIDDEN TABLE NAMES (DO NOT USE):
- ""Plots"" (does not exist - use InventoryDetail)
- ""inventories"" (does not exist - use InventoryDetail)
- ""inventory"" (does not exist - use InventoryDetail)
- ""customers"" (lowercase - use Customers)
- ""payments"" (lowercase - use Payments)
- Any plural or lowercase variations

INSTRUCTIONS:
1. If the user is greeting you or asking a general question (not a database query), respond naturally without generating SQL.
2. If the user asks a database question:
   a. FIRST: Look at the DATABASE SCHEMA above and identify the EXACT table name
   b. SECOND: Identify the EXACT column names from that table
   c. THIRD: Generate SQL using ONLY those exact names
3. SECURITY: You MUST ONLY generate SELECT queries. NEVER generate INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, EXEC, or any data modification statements.
4. For InventoryDetail table (when users ask about inventory/plots):
   - 'Available' means plots/inventory are available
   - 'Allotted' or 'Sold' means plots/inventory are sold/allotted
   - Street column contains road/street information
   - Block column contains block information
   - Project column contains project name (may contain ""Phase"" in the name)
   - SubProject column contains sub-project information
   - There is NO ""phase"" column - check Project or SubProject columns instead
5. When users mention ""Phase"", check the Project or SubProject columns using LIKE, not a separate Phase column.
6. Always use proper SQL syntax with exact table and column names from the schema.
7. Return ONLY the SQL SELECT query, nothing else. No explanations, no markdown, just the SQL.

Example questions and queries (using EXACT table names and SQL Server date functions):
- ""How many inventories in Phase 1?"" → SELECT COUNT(*) FROM InventoryDetail WHERE Project LIKE '%Phase 1%' OR SubProject LIKE '%Phase 1%'
- ""How many plots are available in Road 1 of Block A?"" → SELECT COUNT(*) FROM InventoryDetail WHERE Street = 'Road 1' AND Block = 'A' AND AllotmentStatus = 'Available'
- ""Show me all allotted plots in Block B"" → SELECT * FROM InventoryDetail WHERE Block = 'B' AND AllotmentStatus IN ('Allotted', 'Sold')
- ""How many customers do we have?"" → SELECT COUNT(*) FROM Customers
- ""How many new customers this month?"" → SELECT COUNT(*) FROM Customers WHERE YEAR(CreationDate) = YEAR(GETDATE()) AND MONTH(CreationDate) = MONTH(GETDATE())
- ""How many customers this year?"" → SELECT COUNT(*) FROM Customers WHERE YEAR(CreationDate) = YEAR(GETDATE())
- ""Show me new payments created this month group by bank name?"" → SELECT BankName, COUNT(*) AS NewPaymentsCount FROM Payments WHERE YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE()) AND MONTH(TRY_CONVERT(DATETIME, CreatedOn, 120)) = MONTH(GETDATE()) GROUP BY BankName ORDER BY BankName
- ""Show me payments this month?"" → SELECT * FROM Payments WHERE YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE()) AND MONTH(TRY_CONVERT(DATETIME, CreatedOn, 120)) = MONTH(GETDATE())
- ""Hello"" → (respond naturally, no SQL)";

            // Load configuration limits
            var maxConversationHistory = int.Parse(_configuration["Groq:MaxConversationHistory"] ?? "20");
            var maxMessageLength = int.Parse(_configuration["Groq:MaxMessageLength"] ?? "2000");
            
            // Convert conversation history to ChatMessage format (configurable limit)
            var conversationHistory = request.ConversationHistory?
                .TakeLast(maxConversationHistory) // Configurable limit
                .Select(m => new ChatMessage
                {
                    Role = m.Role,
                    Content = m.Content.Length > maxMessageLength ? m.Content.Substring(0, maxMessageLength) + "..." : m.Content
                })
                .ToList();

            // Step 1: Determine if this is a query or greeting
            var isQueryPrompt = $@"Analyze the following user message and determine if it requires a database query.

User message: ""{request.Message}""

Respond with ONLY one word: 'QUERY' if a database query is needed, or 'GREETING' if it's a general conversation or greeting.

Examples:
- ""How many plots available?"" → QUERY
- ""Hello, how are you?"" → GREETING
- ""Show me customers"" → QUERY
- ""What can you do?"" → GREETING";

            var queryTypeResponse = await _groqService.GenerateResponseAsync(isQueryPrompt, request.Message, conversationHistory);
            var isQuery = queryTypeResponse.Trim().ToUpper().Contains("QUERY");

            // Add delay to prevent rate limiting
            await Task.Delay(3000);

            var response = new ChatMessageViewModel
            {
                Role = "assistant",
                Timestamp = DateTime.Now
            };

            if (isQuery)
            {
                // Step 2: Generate SQL query
                var queryGenerationPrompt = $@"Generate a SQL SELECT query based on the user's question.

CRITICAL RULES:
1. You MUST ONLY generate SELECT queries. NEVER generate INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, or any data modification statements.
2. You MUST use EXACT table names from the database schema provided earlier. Do NOT invent table names or use plural/lowercase variations.
3. For inventory/plots questions, use table name: InventoryDetail (exact case)
4. Check the schema for exact column names before generating the query.
5. DATABASE IS MICROSOFT SQL SERVER - Use SQL Server date functions:
   - Current date: GETDATE() or CURRENT_TIMESTAMP
   - Extract year: YEAR(date_column) - NEVER use EXTRACT(YEAR FROM date_column)
   - Extract month: MONTH(date_column) - NEVER use EXTRACT(MONTH FROM date_column)
   - Extract day: DAY(date_column) - NEVER use EXTRACT(DAY FROM date_column)
   - FOR DATETIME COLUMNS: This month: WHERE YEAR(CreationDate) = YEAR(GETDATE()) AND MONTH(CreationDate) = MONTH(GETDATE())
   - FOR STRING DATE COLUMNS (Payments.CreatedOn, Payments.PaidDate): This month: WHERE YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE()) AND MONTH(TRY_CONVERT(DATETIME, CreatedOn, 120)) = MONTH(GETDATE())
   - FOR DATETIME COLUMNS: This year: WHERE YEAR(CreationDate) = YEAR(GETDATE())
   - FOR STRING DATE COLUMNS: This year: WHERE YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE())
   - FOR DATETIME COLUMNS: Last N days: WHERE CreationDate >= DATEADD(day, -N, GETDATE())
   - FOR STRING DATE COLUMNS: Last N days: WHERE TRY_CONVERT(DATETIME, CreatedOn, 120) >= DATEADD(day, -N, GETDATE())
   - FORBIDDEN: EXTRACT() function does NOT exist in SQL Server - NEVER use EXTRACT()
   - CRITICAL: Payments table date columns (CreatedOn, PaidDate, DSDate, DDDate, ChequeDate) are VARCHAR - ALWAYS use TRY_CONVERT(DATETIME, column_name, 120) before using YEAR() or MONTH()

User question: ""{request.Message}""

Return ONLY a valid SQL SELECT query using EXACT table and column names from the schema and SQL Server date functions, nothing else. No explanations, no markdown, just the SQL SELECT statement.";

                var sqlQuery = await _groqService.GenerateResponseAsync(queryGenerationPrompt, request.Message, conversationHistory);
                
                // Clean up the SQL query (remove markdown code blocks if present)
                sqlQuery = sqlQuery.Trim();
                if (sqlQuery.StartsWith("```sql"))
                {
                    sqlQuery = sqlQuery.Substring(6);
                }
                if (sqlQuery.StartsWith("```"))
                {
                    sqlQuery = sqlQuery.Substring(3);
                }
                if (sqlQuery.EndsWith("```"))
                {
                    sqlQuery = sqlQuery.Substring(0, sqlQuery.Length - 3);
                }
                sqlQuery = sqlQuery.Trim();
                
                // Fix string date column usage in Payments table
                sqlQuery = FixStringDateColumns(sqlQuery);

                // Add delay to prevent rate limiting
                await Task.Delay(3000);

                // Step 2.5: Audit the generated query against database schema
                var auditResult = await AuditQueryAgainstSchemaAsync(sqlQuery, dbSchema, request.Message);
                
                if (!auditResult.IsValid)
                {
                    // If audit failed, try to regenerate the query with the corrected information
                    if (!string.IsNullOrEmpty(auditResult.SuggestedFix))
                    {
                        _logger.LogWarning($"Query audit failed, attempting to fix: {auditResult.Error}");
                        sqlQuery = auditResult.SuggestedFix;
                        
                        // Re-audit the fixed query
                        var reAuditResult = await AuditQueryAgainstSchemaAsync(sqlQuery, dbSchema, request.Message);
                        if (!reAuditResult.IsValid)
                        {
                            response.QueryResult = new QueryResultViewModel
                            {
                                Success = false,
                                Error = $"Query validation failed: {reAuditResult.Error}. Original query: {sqlQuery}",
                                GeneratedQuery = sqlQuery
                            };
                            response.Content = $"I couldn't generate a valid query. {reAuditResult.Error}";
                            return Json(new { success = true, message = response });
                        }
                    }
                    else
                    {
                        response.QueryResult = new QueryResultViewModel
                        {
                            Success = false,
                            Error = auditResult.Error,
                            GeneratedQuery = sqlQuery
                        };
                        response.Content = $"Query validation failed: {auditResult.Error}";
                        return Json(new { success = true, message = response });
                    }
                }

                // Add delay to prevent rate limiting before interpretation step
                await Task.Delay(3000);

                // Step 3: Check if this is a COUNT query and convert it to SELECT * to get actual data
                var originalQuery = sqlQuery;
                var isCountQuery = IsCountQuery(sqlQuery);
                var dataQuery = sqlQuery;
                
                if (isCountQuery)
                {
                    // Convert COUNT query to SELECT * query to fetch actual data
                    dataQuery = ConvertCountToSelectAll(sqlQuery);
                    _logger.LogInformation($"Converted COUNT query to SELECT *: {dataQuery}");
                }

                // Step 3.1: Execute the query (use dataQuery which is SELECT * if it was a COUNT query)
                var queryResult = await _sqlService.ExecuteQueryAsync(dataQuery);

                if (queryResult.Success)
                {
                    // If it was a COUNT query, calculate the count from rows
                    var rowCount = queryResult.Rows.Count;
                    
                    response.QueryResult = new QueryResultViewModel
                    {
                        Success = true,
                        Columns = queryResult.Columns,
                        Rows = queryResult.Rows.Select(r => 
                            r.ToDictionary(kvp => kvp.Key, kvp => kvp.Value == DBNull.Value ? (object?)null : kvp.Value)
                        ).ToList(),
                        GeneratedQuery = originalQuery, // Show original COUNT query
                        ActualDataQuery = isCountQuery ? dataQuery : null // Store the SELECT * query if it was converted
                    };

                    // Step 4: Use AI to interpret and summarize the results in natural language
                    // Load configuration limits
                    var maxResultRows = int.Parse(_configuration["Groq:MaxResultRows"] ?? "100");
                    var maxCellValueLength = int.Parse(_configuration["Groq:MaxCellValueLength"] ?? "500");
                    var maxResultJsonSize = int.Parse(_configuration["Groq:MaxResultJsonSize"] ?? "50000");
                    
                    // Limit result data to prevent payload size issues (configurable limits)
                    var columnsJson = JsonSerializer.Serialize(queryResult.Columns);
                    
                    // Limit rows and truncate large values (configurable limits)
                    var limitedRows = queryResult.Rows.Take(maxResultRows).Select(row => 
                        row.ToDictionary(
                            kvp => kvp.Key, 
                            kvp => {
                                var value = kvp.Value == DBNull.Value ? null : kvp.Value;
                                var strValue = value?.ToString() ?? "";
                                // Truncate individual cell values (configurable limit)
                                return strValue.Length > maxCellValueLength ? strValue.Substring(0, maxCellValueLength) + "..." : value;
                            }
                        )
                    ).ToList();
                    
                    var rowsJson = JsonSerializer.Serialize(limitedRows);
                    
                    // Truncate JSON if still too large (configurable limit)
                    if (rowsJson.Length > maxResultJsonSize)
                    {
                        rowsJson = rowsJson.Substring(0, maxResultJsonSize) + $"...[truncated - showing first {maxResultJsonSize} characters of {rowsJson.Length} total]";
                    }
                    
                    // Build interpretation prompt with emphasis on empty results
                    var emptyResultsInstruction = rowCount == 0 
                        ? "⚠️ CRITICAL: NUMBER OF ROWS IS 0 - NO DATA FOUND. You MUST tell the user that nothing was found and suggest trying a different search."
                        : $"NUMBER OF ROWS: {rowCount} - Data found, provide details.";
                    
                    var interpretationPrompt = $@"You are a helpful AI assistant for a Property Management System. 
A user asked a question, you generated and executed a SQL query, and here are the results.

USER'S QUESTION: ""{request.Message}""
QUERY COLUMNS: {columnsJson}
QUERY RESULTS (showing first 50 rows, as JSON array): {rowsJson}
{emptyResultsInstruction}

INSTRUCTIONS:
1. FIRST CHECK - CRITICAL: If NUMBER OF ROWS is 0 (the query returned no data), you MUST respond EXACTLY with this message: ""No results found in the database matching your search criteria. Please try a different search with different parameters, or check if the data exists with different criteria.""
2. If NUMBER OF ROWS > 0:
   a. Read the query results and extract the actual values from the data.
   b. Provide a direct answer using the actual values from the results.
   c. Format it naturally, like: ""We have 1 customer with NIC number: 42401-2381527-5"" or ""Found 5 available plots in Sector A"".
   d. Include specific values from the results in your response (e.g., actual CNIC numbers, names, plot numbers, etc.).
3. Keep it concise (1-2 sentences).
4. Be friendly and professional.
5. Do NOT mention technical details like table names, SQL, or column names unless specifically asked.
6. If the result is a count query with data, mention the count ({rowCount}) and include relevant details from the actual data rows.

Example responses:
- For 0 rows: ""No results found in the database matching your search criteria. Please try a different search with different parameters, or check if the data exists with different criteria.""
- For COUNT query with 1 row: ""We have 1 customer with NIC number: 42401-2381527-5""
- For COUNT query with 5 rows: ""There are 5 available plots in Sector A""
- For data rows: ""Found 3 customers matching your criteria: [list key details]""";

                    response.Content = await _groqService.GenerateResponseAsync(interpretationPrompt, "", conversationHistory);
                    
                    // Ensure clear message when no rows found
                    if (rowCount == 0)
                    {
                        // Check if the AI response already mentions "no results" or "not found"
                        var contentLower = response.Content.ToLowerInvariant();
                        if (!contentLower.Contains("no result") && 
                            !contentLower.Contains("not found") && 
                            !contentLower.Contains("nothing found") &&
                            !contentLower.Contains("no data") &&
                            !contentLower.Contains("0 result"))
                        {
                            // Override with clear message if AI didn't follow instructions
                            response.Content = "No results found in the database matching your search criteria. Please try a different search with different parameters, or check if the data exists with different criteria.";
                        }
                    }
                }
                else
                {
                    response.QueryResult = new QueryResultViewModel
                    {
                        Success = false,
                        Error = queryResult.Error,
                        GeneratedQuery = sqlQuery
                    };
                    response.Content = $"Error executing query: {queryResult.Error}";
                }
            }
            else
            {
                // General conversation - use AI to respond naturally
                var conversationPrompt = @"You are a helpful AI assistant for a Property Management System. 
Answer user questions naturally and helpfully. If they ask about database capabilities, explain that you can help them query the database by asking questions like 'How many plots are available?' or 'Show me all customers'.";
                
                response.Content = await _groqService.GenerateResponseAsync(conversationPrompt, request.Message, conversationHistory);
            }

            return Json(new { success = true, message = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return Json(new { success = false, error = $"An error occurred: {ex.Message}" });
        }
    }

    private string FixStringDateColumns(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
            return sqlQuery;

        try
        {
            var fixedQuery = sqlQuery;
            
            // List of string date columns in Payments table that need conversion
            var stringDateColumns = new[] { "CreatedOn", "PaidDate", "DSDate", "DDDate", "ChequeDate" };
            
            foreach (var column in stringDateColumns)
            {
                // Fix CONVERT(date, column) -> TRY_CONVERT(DATETIME, column, 120)
                var convertPattern = $@"\bCONVERT\s*\(\s*date\s*,\s*({column})\s*\)";
                fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                    fixedQuery,
                    convertPattern,
                    "TRY_CONVERT(DATETIME, $1, 120)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                // Fix CONVERT(datetime, column) -> TRY_CONVERT(DATETIME, column, 120)
                var convertDateTimePattern = $@"\bCONVERT\s*\(\s*datetime\s*,\s*({column})\s*\)";
                fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                    fixedQuery,
                    convertDateTimePattern,
                    "TRY_CONVERT(DATETIME, $1, 120)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                // Fix YEAR(column) -> YEAR(TRY_CONVERT(DATETIME, column, 120))
                // Only if not already wrapped in TRY_CONVERT
                var yearPattern = $@"\bYEAR\s*\(\s*(?!TRY_CONVERT\()({column})\s*\)";
                fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                    fixedQuery,
                    yearPattern,
                    "YEAR(TRY_CONVERT(DATETIME, $1, 120))",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                // Fix MONTH(column) -> MONTH(TRY_CONVERT(DATETIME, column, 120))
                // Only if not already wrapped in TRY_CONVERT
                var monthPattern = $@"\bMONTH\s*\(\s*(?!TRY_CONVERT\()({column})\s*\)";
                fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                    fixedQuery,
                    monthPattern,
                    "MONTH(TRY_CONVERT(DATETIME, $1, 120))",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                // Fix DAY(column) -> DAY(TRY_CONVERT(DATETIME, column, 120))
                // Only if not already wrapped in TRY_CONVERT
                var dayPattern = $@"\bDAY\s*\(\s*(?!TRY_CONVERT\()({column})\s*\)";
                fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                    fixedQuery,
                    dayPattern,
                    "DAY(TRY_CONVERT(DATETIME, $1, 120))",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }
            
            return fixedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error fixing string date columns: {ex.Message}");
            return sqlQuery;
        }
    }

    private string FixExtractFunction(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
            return sqlQuery;

        try
        {
            var fixedQuery = sqlQuery;
            
            // Replace EXTRACT(YEAR FROM column) with YEAR(column)
            fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                fixedQuery,
                @"EXTRACT\s*\(\s*YEAR\s+FROM\s+([^)]+)\s*\)",
                "YEAR($1)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            // Replace EXTRACT(MONTH FROM column) with MONTH(column)
            fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                fixedQuery,
                @"EXTRACT\s*\(\s*MONTH\s+FROM\s+([^)]+)\s*\)",
                "MONTH($1)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            // Replace EXTRACT(DAY FROM column) with DAY(column)
            fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                fixedQuery,
                @"EXTRACT\s*\(\s*DAY\s+FROM\s+([^)]+)\s*\)",
                "DAY($1)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            // Replace CURRENT_DATE with GETDATE() or CAST(GETDATE() AS DATE)
            fixedQuery = System.Text.RegularExpressions.Regex.Replace(
                fixedQuery,
                @"CURRENT_DATE",
                "CAST(GETDATE() AS DATE)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            return fixedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error fixing EXTRACT function: {ex.Message}");
            return sqlQuery;
        }
    }

    private bool IsCountQuery(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
            return false;

        var upperQuery = sqlQuery.Trim().ToUpperInvariant();
        
        // Check for COUNT(*) or COUNT(column) patterns
        // Match patterns like: SELECT COUNT(*) FROM ... or SELECT COUNT(ColumnName) FROM ...
        var countPattern = @"SELECT\s+COUNT\s*\(";
        return System.Text.RegularExpressions.Regex.IsMatch(upperQuery, countPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private string ConvertCountToSelectAll(string countQuery)
    {
        if (string.IsNullOrWhiteSpace(countQuery))
            return countQuery;

        try
        {
            // Pattern to match: SELECT COUNT(*) or SELECT COUNT(column) [AS alias] FROM ...
            // We want to capture everything from FROM onwards
            var pattern = @"SELECT\s+COUNT\s*\([^)]*\)(?:\s+AS\s+\w+)?\s+(FROM\s+.*)";
            var match = System.Text.RegularExpressions.Regex.Match(countQuery, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (match.Success && match.Groups.Count > 1)
            {
                // Extract everything from FROM clause onwards
                var fromClauseAndBeyond = match.Groups[1].Value.Trim();
                return $"SELECT * {fromClauseAndBeyond}";
            }
            
            // Fallback: try to find FROM clause manually and replace SELECT COUNT(*) with SELECT *
            var fromIndex = countQuery.ToUpperInvariant().IndexOf("FROM");
            if (fromIndex > 0)
            {
                var fromClause = countQuery.Substring(fromIndex);
                return $"SELECT * {fromClause}";
            }
            
            // Last resort: simple replacement
            var simplePattern = @"SELECT\s+COUNT\s*\([^)]*\)";
            var replaced = System.Text.RegularExpressions.Regex.Replace(countQuery, simplePattern, "SELECT *", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return replaced;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error converting COUNT query: {ex.Message}. Using original query.");
            return countQuery;
        }
    }

    private async Task<QueryAuditResult> AuditQueryAgainstSchemaAsync(string sqlQuery, string dbSchema, string userQuestion)
    {
        // First, check for EXTRACT function usage (SQL Server doesn't support it)
        var upperQuery = sqlQuery.ToUpperInvariant();
        if (upperQuery.Contains("EXTRACT"))
        {
            // Try to fix EXTRACT usage automatically
            var fixedQuery = FixExtractFunction(sqlQuery);
            if (fixedQuery != sqlQuery)
            {
                _logger.LogWarning($"Query contains EXTRACT function (not supported in SQL Server). Attempting to fix: {sqlQuery}");
                return new QueryAuditResult
                {
                    IsValid = false,
                    Error = "EXTRACT function is not supported in Microsoft SQL Server. Use YEAR(), MONTH(), or DAY() functions instead.",
                    SuggestedFix = fixedQuery
                };
            }
        }

        var auditPrompt = $@"You are a SQL query auditor for Microsoft SQL Server. Your job is to validate that a SQL query uses correct table and column names from the database schema and SQL Server-compatible functions.

DATABASE TYPE: Microsoft SQL Server (MSSQL)
CRITICAL: EXTRACT() function does NOT exist in SQL Server. Use YEAR(), MONTH(), DAY() instead.

CRITICAL: STRING DATE COLUMNS (VARCHAR):
Some date columns are stored as VARCHAR (string) in the database, NOT as date/datetime types:
- Payments.CreatedOn is VARCHAR(60) - stored as string
- Payments.PaidDate is VARCHAR(60) - stored as string
- Payments.DSDate, DDDate, ChequeDate are VARCHAR(60) - stored as string

FOR STRING DATE COLUMNS, YOU MUST USE TRY_CONVERT:
- Payments.CreatedOn: Use TRY_CONVERT(DATETIME, CreatedOn, 120) before YEAR() or MONTH()
- Payments.PaidDate: Use TRY_CONVERT(DATETIME, PaidDate, 120) before YEAR() or MONTH()
- Example: YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE())
- NEVER use: YEAR(CreatedOn) or MONTH(CreatedOn) directly on string date columns

DATABASE SCHEMA:
{dbSchema}

SQL QUERY TO AUDIT:
{sqlQuery}

USER'S ORIGINAL QUESTION:
{userQuestion}

INSTRUCTIONS:
1. Check if all table names in the SQL query exist in the database schema above.
2. Check if all column names in the SQL query exist in the referenced tables.
3. Check if the query uses SQL Server-compatible functions (NOT EXTRACT - use YEAR, MONTH, DAY instead).
4. If the query uses incorrect table or column names, or incompatible functions, provide a corrected version.
5. Pay special attention to:
   - Table name case sensitivity (e.g., InventoryDetail not inventories or Plots)
   - Column name case sensitivity (e.g., Project not phase, AllotmentStatus not status)
   - When users mention ""Phase"", the correct columns are Project or SubProject (not a Phase column)
   - Date functions: Use YEAR(date), MONTH(date), DAY(date) - NOT EXTRACT(YEAR FROM date)
   - Current date: Use GETDATE() or CURRENT_TIMESTAMP - NOT CURRENT_DATE
   - STRING DATE COLUMNS: Payments.CreatedOn, Payments.PaidDate, etc. are VARCHAR - MUST use TRY_CONVERT(DATETIME, column_name, 120) before YEAR() or MONTH()
   - Example correct: YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE())
   - Example WRONG: YEAR(CreatedOn) = YEAR(GETDATE()) - This will fail because CreatedOn is VARCHAR

RESPOND IN THIS EXACT JSON FORMAT (no other text):
{{
  ""isValid"": true/false,
  ""error"": ""error message if invalid, empty if valid"",
  ""suggestedFix"": ""corrected SQL query if invalid, empty if valid""
}}

Examples:
- Query uses ""inventories"" table → isValid: false, error: ""Table 'inventories' does not exist. Use 'InventoryDetail' instead."", suggestedFix: ""SELECT COUNT(*) FROM InventoryDetail WHERE...""
- Query uses ""Phase"" column → isValid: false, error: ""Column 'Phase' does not exist in InventoryDetail. Use 'Project' or 'SubProject' with LIKE operator."", suggestedFix: ""SELECT COUNT(*) FROM InventoryDetail WHERE Project LIKE '%Phase 1%' OR SubProject LIKE '%Phase 1%'""
- Query uses EXTRACT(YEAR FROM date) → isValid: false, error: ""EXTRACT function not supported in SQL Server. Use YEAR(date) instead."", suggestedFix: ""SELECT COUNT(*) FROM Customers WHERE YEAR(CreationDate) = YEAR(GETDATE()) AND MONTH(CreationDate) = MONTH(GETDATE())""
- Query uses YEAR(CreatedOn) on Payments table → isValid: false, error: ""CreatedOn in Payments table is VARCHAR. Use TRY_CONVERT(DATETIME, CreatedOn, 120) before YEAR()."", suggestedFix: ""SELECT BankName, COUNT(*) FROM Payments WHERE YEAR(TRY_CONVERT(DATETIME, CreatedOn, 120)) = YEAR(GETDATE()) AND MONTH(TRY_CONVERT(DATETIME, CreatedOn, 120)) = MONTH(GETDATE()) GROUP BY BankName""
- Query is correct → isValid: true, error: """", suggestedFix: """"";

        try
        {
            var auditResponse = await _groqService.GenerateResponseAsync(auditPrompt, "", null);
            
            // Clean up the response (remove markdown if present)
            auditResponse = auditResponse.Trim();
            if (auditResponse.StartsWith("```json"))
            {
                auditResponse = auditResponse.Substring(7);
            }
            if (auditResponse.StartsWith("```"))
            {
                auditResponse = auditResponse.Substring(3);
            }
            if (auditResponse.EndsWith("```"))
            {
                auditResponse = auditResponse.Substring(0, auditResponse.Length - 3);
            }
            auditResponse = auditResponse.Trim();

            // Parse JSON response
            var auditResult = JsonSerializer.Deserialize<QueryAuditResult>(auditResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return auditResult ?? new QueryAuditResult { IsValid = false, Error = "Failed to parse audit response" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auditing query");
            return new QueryAuditResult { IsValid = false, Error = $"Audit error: {ex.Message}" };
        }
    }
}

public class QueryAuditResult
{
    public bool IsValid { get; set; }
    public string Error { get; set; } = string.Empty;
    public string SuggestedFix { get; set; } = string.Empty;
}
