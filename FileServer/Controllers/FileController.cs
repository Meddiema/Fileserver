using FileServer.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Web;

namespace FileServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly SupabaseStorageService _storageService;

        public FileController()
        {
            _storageService = new SupabaseStorageService();
        }

#if !DEBUG
        // 👇 Hide from Swagger in production (to prevent 500 errors)
        [ApiExplorerSettings(IgnoreApi = true)]
#endif
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                using var stream = file.OpenReadStream();
                var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);

                return Ok(new
                {
                    Name = file.FileName,
                    Size = file.Length,
                    Url = url,
                    Uploaded = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
            }
        }

        // ✅ List all files in Supabase bucket
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            try
            {
                var fileTuples = await _storageService.ListFilesAsync();

                var files = fileTuples.Select(f => new
                {
                    Name = f.Name,
                    Size = f.Size,
                    PublicUrl = f.PublicUrl,
                    DownloadUrl = $"{Request.Scheme}://{Request.Host}/api/file/download?url={Uri.EscapeDataString(f.PublicUrl)}",
                    Uploaded = DateTime.UtcNow
                });

                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to list files: {ex.Message}" });
            }
        }

        // ✅ Download a file using the full public URL
        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Invalid download URL.");

            try
            {
                var decodedUrl = Uri.UnescapeDataString(HttpUtility.UrlDecode(url));

                if (!decodedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Invalid Supabase file URL.");

                var stream = await _storageService.DownloadFromUrlAsync(decodedUrl);

                var fileName = Path.GetFileName(new Uri(decodedUrl).LocalPath);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "File not found on Supabase." });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = $"Network error while downloading: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Download failed: {ex.Message}" });
            }
        }
    }
}
