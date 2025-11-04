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

        /// <summary>
        /// Upload a file to Supabase Storage (hidden from Swagger UI)
        /// </summary>
        [HttpPost("upload")]
        [ApiExplorerSettings(IgnoreApi = true)]  // ✅ hides from Swagger but still works
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);

            return Ok(new
            {
                fileName = file.FileName,
                length = file.Length,
                url
            });
        }

        /// <summary>
        /// List all uploaded files
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _storageService.ListFilesAsync();
            return Ok(files);
        }

        /// <summary>
        /// Get a direct public download link for a file
        /// </summary>
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Invalid file name.");

            var url = await _storageService.GetPublicUrlAsync(fileName);
            if (string.IsNullOrEmpty(url))
                return NotFound();

            return Redirect(url); // redirects user to Supabase public URL
        }
    }
}
