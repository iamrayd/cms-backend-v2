using ProjectCms.Services;
using ProjectCms.Api.Services;
using ProjectCms.Models;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// LOGGING CONFIGURATION
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// CONTROLLERS + API DOCUMENTATION
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Project CMS API",
        Version = "v1",
        Description = "Content Management System API with Pages, Posts, and Banners"
    });
});

// MONGODB CONFIGURATION
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

// Register services
builder.Services.AddSingleton<PageService>();
builder.Services.AddSingleton<PostService>();
builder.Services.AddSingleton<BannerService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<IActivityLogService, ActivityLogService>();
builder.Services.AddSingleton<ArchivedBannerService>();
builder.Services.AddSingleton<ArchivedPageService>();

// Background services
builder.Services.AddHostedService<BannerExpiryWorker>();

// HTTP CONTEXT ACCESSOR (for getting current user)
builder.Services.AddHttpContextAccessor();

// CORS CONFIGURATION
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:4200" };

        policy
            .WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization")
            .AllowCredentials();
    });
});

// RESPONSE COMPRESSION (Optional)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// BUILD APP
var app = builder.Build();

// EXCEPTION HANDLING MIDDLEWARE
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Project CMS API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}
else
{
    // Production error handling
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Global error endpoint
app.MapGet("/error", (HttpContext context) =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    if (error != null)
    {
        logger.LogError(error.Error, "Unhandled exception occurred");
    }

    return Results.Problem(
        title: "An error occurred",
        statusCode: StatusCodes.Status500InternalServerError,
        detail: app.Environment.IsDevelopment() ? error?.Error.Message : "Please try again later"
    );
});

// MIDDLEWARE PIPELINE
app.UseHttpsRedirection();

// Response compression (if enabled)
app.UseResponseCompression();

// CORS - Must be before Authorization
app.UseCors("AllowAngular");

app.UseAuthorization();

// HEALTH CHECK ENDPOINT
app.MapGet("/health", async (PageService pageService) =>
{
    try
    {
        await pageService.GetAsync();
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = "connected"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                database = "disconnected",
                error = ex.Message
            },
            statusCode: 503
        );
    }
});

// MAP CONTROLLERS
app.MapControllers();

// STARTUP LOGGING
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting up...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("CORS allowed origins: {Origins}",
    string.Join(", ", builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" }));

app.Run();