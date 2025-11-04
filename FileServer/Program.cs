using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using FileServer.Filters;

var builder = WebApplication.CreateBuilder(args);

// ✅ Allow large uploads (up to 2 GB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2 GB
});

// ✅ Increase Kestrel limits for large uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2 GB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
    options.ListenAnyIP(8080); // Force port 8080 for Render
});

// ✅ Add Controllers + Swagger + Filter
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<FileUploadOperationFilter>();
});

// ✅ Enable CORS (for mobile & web)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ✅ Swagger always visible
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileServer API v1");
    c.RoutePrefix = string.Empty; // Open Swagger at root
});

// ✅ CORS + middleware
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// ✅ Ensure Uploads folder exists (local testing only)
var uploadPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadPath))
    Directory.CreateDirectory(uploadPath);

// ✅ Serve static uploaded files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

app.Run();
