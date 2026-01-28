using System.Text;
using System.Text.Json;

namespace PMS.Web.Services;

public class GroqAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly double _temperature;
    private readonly IConfiguration _configuration;

    public GroqAIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _apiKey = configuration["Groq:ApiKey"] ?? "YOUR_GROQ_API_KEY";
        _model = configuration["Groq:Model"] ?? "openai/gpt-oss-120b";
        _temperature = double.Parse(configuration["Groq:Temperature"] ?? "0.1");
        
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, List<ChatMessage>? conversationHistory = null)
    {
        // Load configuration limits
        var maxSystemPromptSize = int.Parse(_configuration["Groq:MaxSystemPromptSize"] ?? "30000");
        var maxConversationHistory = int.Parse(_configuration["Groq:MaxConversationHistory"] ?? "20");
        var maxMessageLength = int.Parse(_configuration["Groq:MaxMessageLength"] ?? "2000");
        var maxPayloadSize = int.Parse(_configuration["Groq:MaxPayloadSize"] ?? "1000000");
        
        var messages = new List<object>();
        
        // Truncate system prompt if too large (configurable limit)
        var truncatedSystemPrompt = systemPrompt.Length > maxSystemPromptSize 
            ? systemPrompt.Substring(0, maxSystemPromptSize) + $"\n\n[System prompt truncated for size - showing first {maxSystemPromptSize} characters of {systemPrompt.Length} total...]"
            : systemPrompt;
        
        // Add system message
        messages.Add(new { role = "system", content = truncatedSystemPrompt });
        
        // Add conversation history if provided (configurable limit)
        if (conversationHistory != null)
        {
            foreach (var msg in conversationHistory.Take(maxConversationHistory))
            {
                var truncatedContent = msg.Content.Length > maxMessageLength 
                    ? msg.Content.Substring(0, maxMessageLength) + "..."
                    : msg.Content;
                messages.Add(new { role = msg.Role, content = truncatedContent });
            }
        }
        
        // Truncate user message if too large (configurable limit)
        var truncatedUserMessage = userMessage.Length > maxMessageLength 
            ? userMessage.Substring(0, maxMessageLength) + "..."
            : userMessage;
        
        // Add current user message
        messages.Add(new { role = "user", content = truncatedUserMessage });

        var requestBody = new
        {
            model = _model,
            messages = messages,
            temperature = _temperature
        };

        var json = JsonSerializer.Serialize(requestBody);
        
        // Check payload size (configurable limit, default 1MB)
        if (json.Length > maxPayloadSize)
        {
            throw new Exception($"Request payload too large ({json.Length} bytes, limit: {maxPayloadSize} bytes). Please try a shorter question, clear chat history, or increase MaxPayloadSize in appsettings.json.");
        }
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseObj.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? string.Empty;
                }
            }

            throw new Exception("Invalid response format from Groq API");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error calling Groq API: {ex.Message}", ex);
        }
    }
}

public class ChatMessage
{
    public string Role { get; set; } = "user"; // "user", "assistant", or "system"
    public string Content { get; set; } = string.Empty;
}
