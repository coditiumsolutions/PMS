using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.MapStaticAssets();

// Map attribute-routed controllers first
app.MapControllers();

// Map conventional routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
