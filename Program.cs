using ProjectCms.Services;
using ProjectCms.Api.Services;
using ProjectCms.Models;
using ProjectCms.Services;
using ProjectCms.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<PageService>(); // Page Serviceget
builder.Services.AddSingleton<PostService>(); // Post Service
builder.Services.AddSingleton<BannerService>(); //Banner Service
builder.Services.AddSingleton<UserService>(); // User Service
builder.Services.AddHostedService<BannerExpiryWorker>(); //Banner Expirey
builder.Services.AddSingleton<IActivityLogService, ActivityLogService>(); //Activity-log
builder.Services.AddSingleton<ArchivedBannerService>(); //ArchivedBanner Service
builder.Services.AddSingleton<ArchivedPageService>(); //ArchivedPage Service





// 3. CORS for Angular dev (http://localhost:4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// 4. Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. Pipeline
app.UseHttpsRedirection();

app.UseCors("AllowAngular");   // *** IMPORTANT: CORS before Authorization ***

app.UseAuthorization();

app.MapControllers();

app.Run();