namespace PMS.Web.ViewModels;

public class ChatMessageViewModel
{
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public QueryResultViewModel? QueryResult { get; set; }
}

public class QueryResultViewModel
{
    public bool Success { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object>> Rows { get; set; } = new();
    public string? Error { get; set; }
    public string? GeneratedQuery { get; set; }
}

public class ChatRequestViewModel
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageViewModel>? ConversationHistory { get; set; }
}
