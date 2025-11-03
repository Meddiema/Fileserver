using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly string _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!Directory.Exists(_uploadDir))
            Directory.CreateDirectory(_uploadDir);

        var token = Guid.NewGuid().ToString("N");
        var originalFileName = Path.GetFileName(file.FileName);
        var savePath = Path.Combine(_uploadDir, $"{token}_{originalFileName}");

        using (var stream = new FileStream(savePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var metadata = new
        {
            token,
            name = originalFileName,
            size = file.Length,
            uploaded = DateTime.UtcNow
        };

        System.IO.File.WriteAllText(
            Path.Combine(_uploadDir, $"{token}.json"),
            JsonSerializer.Serialize(metadata)
        );

        return Ok(metadata);
    }

    [HttpGet("list")]
    public IActionResult List()
    {
        var files = Directory.GetFiles(_uploadDir, "*.json")
            .Select(f => JsonSerializer.Deserialize<dynamic>(System.IO.File.ReadAllText(f)))
            .ToList();

        return Ok(files);
    }

    [HttpGet("download/{token}")]
    public IActionResult Download(string token)
    {
        var jsonPath = Path.Combine(_uploadDir, $"{token}.json");
        if (!System.IO.File.Exists(jsonPath))
            return NotFound("File not found");

        var meta = JsonSerializer.Deserialize<dynamic>(System.IO.File.ReadAllText(jsonPath));
        string name = meta?.GetProperty("name").GetString();
        string fullPath = Path.Combine(_uploadDir, $"{token}_{name}");

        if (!System.IO.File.Exists(fullPath))
            return NotFound("File not found on disk");

        var mimeType = "application/octet-stream";
        return PhysicalFile(fullPath, mimeType, name);
    }
}
