using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

// Allow large file uploads
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = long.MaxValue);

var app = builder.Build();

app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseSwagger();
app.UseSwaggerUI();

// Upload directory
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
Directory.CreateDirectory(uploadDir);

// ---------- Upload ----------
app.MapPost("/api/files/upload", async (HttpRequest req) =>
{
    if (!req.HasFormContentType)
        return Results.BadRequest("Expecting form-data");

    var form = await req.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file == null)
        return Results.BadRequest("No file received");

    var token = Guid.NewGuid().ToString("N");
    var savedName = $"{token}_{file.FileName}";
    var path = Path.Combine(uploadDir, savedName);

    using var fs = new FileStream(path, FileMode.Create);
    await file.CopyToAsync(fs);

    var meta = new { token, name = file.FileName, size = file.Length, uploaded = DateTime.UtcNow };
    await File.WriteAllTextAsync(Path.Combine(uploadDir, $"{token}.json"),
        System.Text.Json.JsonSerializer.Serialize(meta));

    return Results.Ok(meta);
});

// ---------- Download ----------
app.MapGet("/api/files/download/{token}", (string token) =>
{
    var metaFile = Path.Combine(uploadDir, $"{token}.json");
    if (!File.Exists(metaFile)) return Results.NotFound();

    var fileName = Directory.GetFiles(uploadDir)
        .FirstOrDefault(f => Path.GetFileName(f).StartsWith(token));

    if (fileName == null) return Results.NotFound();
    return Results.File(fileName, "application/octet-stream", Path.GetFileName(fileName));
});

app.Run();
