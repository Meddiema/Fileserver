using FileServer.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

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

        // ✅ Upload endpoint (HIDDEN from Swagger to avoid 500 error)
#if !DEBUG
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

        // ✅ List files (shows proper names + direct download links)
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            try
            {
                var urls = await _storageService.ListFilesAsync();

                var files = urls.Select(u =>
                {
                    var name = Path.GetFileName(u);
                    return new
                    {
                        Name = name,
                        DownloadUrl = $"{Request.Scheme}://{Request.Host}/api/file/download/{Uri.EscapeDataString(name)}",
                        PublicUrl = u,
                        Uploaded = DateTime.UtcNow
                    };
                });

                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to list files: {ex.Message}" });
            }
        }

        // ✅ Download a file (works for mobile or browser)
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Invalid file name.");

            try
            {
                var stream = await _storageService.DownloadAsync(fileName);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "File not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Download failed: {ex.Message}" });
            }
        }
    }
}
