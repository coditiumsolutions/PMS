using System.Text;
using System.Text.Json;

namespace PMS.Web.Services;

public class GroqAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly double _temperature;

    public GroqAIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] ?? "YOUR_GROQ_API_KEY";
        _model = configuration["Groq:Model"] ?? "openai/gpt-oss-120b";
        _temperature = double.Parse(configuration["Groq:Temperature"] ?? "0.1");
        
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, List<ChatMessage>? conversationHistory = null)
    {
        var messages = new List<object>();
        
        // Add system message
        messages.Add(new { role = "system", content = systemPrompt });
        
        // Add conversation history if provided
        if (conversationHistory != null)
        {
            foreach (var msg in conversationHistory)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }
        }
        
        // Add current user message
        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model = _model,
            messages = messages,
            temperature = _temperature
        };

        var json = JsonSerializer.Serialize(requestBody);
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
