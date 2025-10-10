using Microsoft.EntityFrameworkCore;
using TestAutomationApp.API.Data;
using TestAutomationApp.API.Models;
using TestAutomationApp.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=testautomation.db"));

// Register services
builder.Services.AddScoped<ITestGeneratorService, TestGeneratorService>();
builder.Services.AddScoped<IPageAnalyzerService, PageAnalyzerService>();
builder.Services.AddScoped<ITestScenarioService, TestScenarioService>();
builder.Services.AddScoped<ITestDataService, TestDataService>();
builder.Services.AddScoped<ITestExecutorService, TestExecutorService>();
builder.Services.AddSingleton<BrowserAutomationService>();
builder.Services.AddScoped<IPlaywrightExecutorService, PlaywrightExecutorService>();
builder.Services.AddHttpClient();

// Configure HRSA Settings
builder.Services.Configure<HrsaSettings>(builder.Configuration.GetSection("HrsaCredentials"));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient",
        policy =>
        {
            policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazorClient");

app.UseAuthorization();

app.MapControllers();

app.Run();
