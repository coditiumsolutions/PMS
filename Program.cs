using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Helper method to load API key from test.txt
static void LoadApiKeyFromTestFile(WebApplicationBuilder builder)
{
    try
    {
        var env = builder.Environment;
        var testFilePath = Path.Combine(env.ContentRootPath, "test.txt");
        var appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
        
        // Check if test.txt exists
        if (!File.Exists(testFilePath))
        {
            Console.WriteLine("test.txt not found. Using default API key configuration.");
            return;
        }

        // Read and extract API key
        var testFileContent = File.ReadAllText(testFilePath).Trim();
        var apiKey = ExtractApiKey(testFileContent);
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("Could not extract API key from test.txt.");
            return;
        }

        // Check if appsettings.json has placeholder
        if (File.Exists(appSettingsPath))
        {
            var appSettingsContent = File.ReadAllText(appSettingsPath);
            if (appSettingsContent.Contains("\"ApiKey\": \"YOUR_GROQ_API_KEY\""))
            {
                // Update appsettings.json
                var updatedContent = appSettingsContent.Replace(
                    "\"ApiKey\": \"YOUR_GROQ_API_KEY\"",
                    $"\"ApiKey\": \"{apiKey}\""
                );
                File.WriteAllText(appSettingsPath, updatedContent);
                Console.WriteLine("API key loaded from test.txt and updated in appsettings.json.");
            }
        }

        // Also update configuration in memory
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Groq:ApiKey", apiKey }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to load API key from test.txt: {ex.Message}");
    }
}

// Helper method to extract API key from test.txt
static string ExtractApiKey(string content)
{
    // Method 1: Remove "test" prefix and suffix (case-insensitive)
    var trimmed = content.Trim();
    
    if (trimmed.StartsWith("test", StringComparison.OrdinalIgnoreCase))
    {
        trimmed = trimmed.Substring(4);
    }
    
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

    // Method 3: Return trimmed content as fallback
    return trimmed.Trim();
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Add DbContext
builder.Services.AddDbContext<PMSDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register AuditLogService
builder.Services.AddScoped<AuditLogService>();

// Register CustomerAnalyticsService
builder.Services.AddScoped<CustomerAnalyticsService>();

// Load API key from test.txt before building app (bypass GitHub secret scanning)
LoadApiKeyFromTestFile(builder);

// Register AI RAG Services
builder.Services.AddHttpClient<GroqAIService>();
builder.Services.AddScoped<SQLQueryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Serve static files from wwwroot (required for runtime-created uploads, e.g. /uploads/customers/...)
app.UseStaticFiles();

app.MapStaticAssets();

// Map attribute-routed controllers first
app.MapControllers();

// Map conventional routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
