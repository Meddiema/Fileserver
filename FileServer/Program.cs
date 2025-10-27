using FileServer.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add controller support
builder.Services.AddControllers();

// Add Swagger with IFormFile mapping
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileServer API", Version = "v1" });

    // 👇 Tell Swagger how to handle file uploads
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

// Register FileStorageService
builder.Services.AddScoped<FileStorageService>();

// Allow CORS (useful for mobile testing)
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Allow big uploads
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = long.MaxValue);

var app = builder.Build();

app.UseCors();

// ✅ Always enable Swagger (even in production, for Render)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileServer API v1");
    c.RoutePrefix = string.Empty; // ✅ Shows Swagger UI at root URL
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
