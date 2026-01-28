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

    public AIRAGController(
        GroqAIService groqService,
        SQLQueryService sqlService,
        ILogger<AIRAGController> logger)
    {
        _groqService = groqService;
        _sqlService = sqlService;
        _logger = logger;
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

            // Load database schema from db.txt file
            var dbSchemaPath = Path.Combine(Directory.GetCurrentDirectory(), "db.txt");
            var dbSchema = "DATABASE SCHEMA NOT FOUND";
            if (System.IO.File.Exists(dbSchemaPath))
            {
                dbSchema = await System.IO.File.ReadAllTextAsync(dbSchemaPath);
            }
            else
            {
                // Fallback to dynamic schema if file doesn't exist
                dbSchema = await _sqlService.GetDatabaseSchemaAsync();
            }

            // Build system prompt with database schema
            var systemPrompt = $@"You are an AI assistant that helps users query a Property Management System (PMS) database.

CRITICAL SECURITY RULES:
- YOU MUST ONLY GENERATE SELECT QUERIES
- NEVER generate INSERT, UPDATE, DELETE, DROP, ALTER, CREATE, TRUNCATE, or any data modification statements
- If a user asks to modify data, politely explain that you can only read data, not modify it
- Only SELECT statements are allowed - this is enforced at multiple security layers

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

Example questions and queries (using EXACT table names):
- ""How many inventories in Phase 1?"" → SELECT COUNT(*) FROM InventoryDetail WHERE Project LIKE '%Phase 1%' OR SubProject LIKE '%Phase 1%'
- ""How many plots are available in Road 1 of Block A?"" → SELECT COUNT(*) FROM InventoryDetail WHERE Street = 'Road 1' AND Block = 'A' AND AllotmentStatus = 'Available'
- ""Show me all allotted plots in Block B"" → SELECT * FROM InventoryDetail WHERE Block = 'B' AND AllotmentStatus IN ('Allotted', 'Sold')
- ""How many customers do we have?"" → SELECT COUNT(*) FROM Customers
- ""Hello"" → (respond naturally, no SQL)";

            // Convert conversation history to ChatMessage format
            var conversationHistory = request.ConversationHistory?
                .Select(m => new ChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
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

User question: ""{request.Message}""

Return ONLY a valid SQL SELECT query using EXACT table and column names from the schema, nothing else. No explanations, no markdown, just the SQL SELECT statement.";

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

                // Step 3: Execute the query
                var queryResult = await _sqlService.ExecuteQueryAsync(sqlQuery);

                if (queryResult.Success)
                {
                    response.QueryResult = new QueryResultViewModel
                    {
                        Success = true,
                        Columns = queryResult.Columns,
                        Rows = queryResult.Rows.Select(r => 
                            r.ToDictionary(kvp => kvp.Key, kvp => kvp.Value == DBNull.Value ? (object?)null : kvp.Value)
                        ).ToList(),
                        GeneratedQuery = sqlQuery
                    };

                    // Step 4: Use AI to interpret and summarize the results in natural language
                    var interpretationPrompt = $@"You are a helpful AI assistant for a Property Management System. 
A user asked a question, you generated and executed a SQL query, and here are the results.
Summarize these results in 1-2 natural, friendly lines that directly answer the user's question.

USER'S QUESTION: ""{request.Message}""
SQL QUERY EXECUTED: ""{sqlQuery}""
QUERY RESULTS: 
{JsonSerializer.Serialize(queryResult.Rows)}

INSTRUCTIONS:
1. Provide a direct, concise answer to the user's question based on the results.
2. Keep it to 1-2 lines.
3. Be friendly and professional.
4. Do NOT mention technical details like table names or SQL unless specifically asked.";

                    response.Content = await _groqService.GenerateResponseAsync(interpretationPrompt, "", conversationHistory);
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

    private async Task<QueryAuditResult> AuditQueryAgainstSchemaAsync(string sqlQuery, string dbSchema, string userQuestion)
    {
        var auditPrompt = $@"You are a SQL query auditor. Your job is to validate that a SQL query uses correct table and column names from the database schema.

DATABASE SCHEMA:
{dbSchema}

SQL QUERY TO AUDIT:
{sqlQuery}

USER'S ORIGINAL QUESTION:
{userQuestion}

INSTRUCTIONS:
1. Check if all table names in the SQL query exist in the database schema above.
2. Check if all column names in the SQL query exist in the referenced tables.
3. If the query uses incorrect table or column names, provide a corrected version.
4. Pay special attention to:
   - Table name case sensitivity (e.g., InventoryDetail not inventories or Plots)
   - Column name case sensitivity (e.g., Project not phase, AllotmentStatus not status)
   - When users mention ""Phase"", the correct columns are Project or SubProject (not a Phase column)

RESPOND IN THIS EXACT JSON FORMAT (no other text):
{{
  ""isValid"": true/false,
  ""error"": ""error message if invalid, empty if valid"",
  ""suggestedFix"": ""corrected SQL query if invalid, empty if valid""
}}

Examples:
- Query uses ""inventories"" table → isValid: false, error: ""Table 'inventories' does not exist. Use 'InventoryDetail' instead."", suggestedFix: ""SELECT COUNT(*) FROM InventoryDetail WHERE...""
- Query uses ""Phase"" column → isValid: false, error: ""Column 'Phase' does not exist in InventoryDetail. Use 'Project' or 'SubProject' with LIKE operator."", suggestedFix: ""SELECT COUNT(*) FROM InventoryDetail WHERE Project LIKE '%Phase 1%' OR SubProject LIKE '%Phase 1%'""
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
