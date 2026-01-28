using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace PMS.Web.Services;

public class ApiKeyLoaderService
{
    private readonly ILogger<ApiKeyLoaderService> _logger;
    private readonly string _appSettingsPath;
    private readonly string _testFilePath;
    private readonly IConfiguration _configuration;

    public ApiKeyLoaderService(ILogger<ApiKeyLoaderService> logger, IWebHostEnvironment env, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        var contentRoot = env.ContentRootPath;
        _appSettingsPath = Path.Combine(contentRoot, "appsettings.json");
        _testFilePath = Path.Combine(contentRoot, "test.txt");
    }

    public string? LoadApiKeyFromTestFile()
    {
        try
        {
            // Check current API key from configuration
            var currentApiKey = _configuration["Groq:ApiKey"];

            if (currentApiKey != "YOUR_GROQ_API_KEY")
            {
                _logger.LogInformation("API key already configured. Skipping update.");
                return currentApiKey;
            }

            // Read test.txt
            if (!File.Exists(_testFilePath))
            {
                _logger.LogWarning("test.txt not found. Cannot load API key.");
                return null;
            }

            var testFileContent = File.ReadAllText(_testFilePath).Trim();
            
            // Extract API key by removing "test" prefix and suffix
            var apiKey = ExtractApiKey(testFileContent);
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Could not extract API key from test.txt. File format may be incorrect.");
                return null;
            }

            // Update appsettings.json file
            UpdateAppSettings(apiKey);
            
            _logger.LogInformation("API key successfully loaded from test.txt and updated in appsettings.json.");
            return apiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API key from test.txt");
            return null;
        }
    }

    private string ExtractApiKey(string content)
    {
        // Method 1: Remove "test" prefix and suffix (case-insensitive)
        var trimmed = content.Trim();
        
        // Remove "test" from start (case-insensitive)
        if (trimmed.StartsWith("test", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(4);
        }
        
        // Remove "test" from end (case-insensitive)
        if (trimmed.EndsWith("test", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 4);
        }

        // Method 2: Use regex to find Groq API key pattern (starts with gsk_)
        var regex = new Regex(@"gsk_[A-Za-z0-9]{32,}", RegexOptions.IgnoreCase);
        var match = regex.Match(trimmed);
        if (match.Success)
        {
            return match.Value;
        }

        // Method 3: If no pattern match, return trimmed content (fallback)
        return trimmed.Trim();
    }

    private void UpdateAppSettings(string apiKey)
    {
        try
        {
            // Read current appsettings.json
            var jsonContent = File.ReadAllText(_appSettingsPath);
            
            // Parse JSON
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;
            
            // Create a dictionary to hold the updated values
            var updatedJson = new Dictionary<string, object>();
            
            // Copy all existing properties
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "Groq")
                {
                    // Update Groq section
                    var groqDict = new Dictionary<string, object>();
                    foreach (var groqProp in property.Value.EnumerateObject())
                    {
                        if (groqProp.Name == "ApiKey")
                        {
                            groqDict["ApiKey"] = apiKey;
                        }
                        else
                        {
                            groqDict[groqProp.Name] = groqProp.Value.GetRawText().Trim('"');
                        }
                    }
                    updatedJson["Groq"] = groqDict;
                }
                else
                {
                    // Copy other sections as-is
                    updatedJson[property.Name] = property.Value.GetRawText();
                }
            }

            // Write back to file with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            var updatedJsonString = JsonSerializer.Serialize(updatedJson, options);
            File.WriteAllText(_appSettingsPath, updatedJsonString);
            
            // Reload configuration
            // Note: Configuration is already loaded, but this ensures the file is updated for next run
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appsettings.json");
            throw;
        }
    }
}
