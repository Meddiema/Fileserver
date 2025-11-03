using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FileServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly string _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        public FileController()
        {
            if (!Directory.Exists(_uploadDir))
                Directory.CreateDirectory(_uploadDir);
        }

        // ✅ Upload endpoint — handles any file type, up to 2 GB
        [HttpPost("upload")]
        [RequestSizeLimit(2L * 1024L * 1024L * 1024L)] // 2 GB
        [RequestFormLimits(MultipartBodyLengthLimit = 2L * 1024L * 1024L * 1024L)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file provided");

                var token = Guid.NewGuid().ToString("N");
                var fileName = file.FileName;
                var savePath = Path.Combine(_uploadDir, $"{token}_{fileName}");

                Console.WriteLine($"[Upload] Saving file: {fileName} ({file.Length} bytes)");

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var meta = new
                {
                    token,
                    name = fileName,
                    size = file.Length,
                    uploaded = DateTime.UtcNow,
                    path = savePath
                };

                var jsonPath = Path.Combine(_uploadDir, $"{token}.json");
                await System.IO.File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(meta));

                Console.WriteLine($"[Upload] File saved successfully: {savePath}");

                return Ok(meta);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Upload] Error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }

        // ✅ Download endpoint — now logs every step
        [HttpGet("download/{token}")]
        public IActionResult Download(string token)
        {
            try
            {
                Console.WriteLine($"[Download] Requested token: {token}");

                var jsonPath = Path.Combine(_uploadDir, $"{token}.json");
                if (!System.IO.File.Exists(jsonPath))
                {
                    Console.WriteLine($"[Download] Metadata not found for {token}");
                    return NotFound("Metadata not found");
                }

                var jsonContent = System.IO.File.ReadAllText(jsonPath);
                var meta = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                var fileName = meta.GetProperty("name").GetString();
                var fullPath = Path.Combine(_uploadDir, $"{token}_{fileName}");

                Console.WriteLine($"[Download] Looking for file: {fullPath}");

                if (!System.IO.File.Exists(fullPath))
                {
                    Console.WriteLine($"[Download] File not found: {fullPath}");
                    return NotFound($"File {fileName} not found");
                }

                var mimeType = "application/octet-stream";
                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                Console.WriteLine($"[Download] Returning file: {fileName}");
                return File(stream, mimeType, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Download] Error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }

        // ✅ List all uploaded files (for debugging)
        [HttpGet("list")]
        public IActionResult ListFiles()
        {
            try
            {
                var files = Directory.GetFiles(_uploadDir, "*.json")
                    .Select(jsonPath =>
                    {
                        var meta = JsonSerializer.Deserialize<JsonElement>(System.IO.File.ReadAllText(jsonPath));
                        return new
                        {
                            token = meta.GetProperty("token").GetString(),
                            name = meta.GetProperty("name").GetString(),
                            size = meta.GetProperty("size").GetInt64(),
                            uploaded = meta.GetProperty("uploaded").GetDateTime()
                        };
                    })
                    .OrderByDescending(f => f.uploaded)
                    .ToList();

                Console.WriteLine($"[ListFiles] Returning {files.Count} entries");
                return Ok(files);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ListFiles] Error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
