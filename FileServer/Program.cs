using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using FileServer.Filters;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Allow large uploads (up to 2 GB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2 GB
});

// ✅ Configure Kestrel for Render (port 8080)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // Render requires port 8080
    options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
});

// ✅ Add Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FileServer API",
        Version = "v1"
    });

    // ✅ Register File Upload Operation Filter
    c.OperationFilter<FileUploadOperationFilter>();
});

// ✅ Enable CORS (for mobile apps)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ✅ Middleware order matters

app.UseHttpsRedirection();

// ✅ Enable CORS globally
app.UseCors("AllowAll");

// ✅ Serve static files (optional)
var uploadPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

// ✅ Always show Swagger (even in production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileServer API v1");
    c.RoutePrefix = string.Empty; // Swagger opens at root
});

app.UseAuthorization();
app.MapControllers();

app.Run();
