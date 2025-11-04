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

        // ✅ Upload endpoint
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

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

        // ✅ List files (return structured objects)
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var urls = await _storageService.ListFilesAsync();

            var files = urls.Select(u => new
            {
                Token = Path.GetFileName(u),   // simple identifier
                Name = Path.GetFileName(u),
                Size = 0,                      // size not available via API
                Path = u,
                Uploaded = DateTime.UtcNow     // placeholder
            });

            return Ok(files);
        }

        // ✅ Download by filename
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Invalid file name.");

            var stream = await _storageService.DownloadAsync(fileName);
            return File(stream, "application/octet-stream", fileName);
        }
    }
}
