using FileServer.Services;
using Microsoft.AspNetCore.Mvc;

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

        // ✅ Upload endpoint (already working on mobile)
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);

            return Ok(new { FileName = file.FileName, Url = url });
        }

        // ✅ List files (for Received Files page)
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _storageService.ListFilesAsync();
            return Ok(files);
        }

        // ✅ Download file by filename
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
