using Microsoft.AspNetCore.Mvc;
using FileServer.Services;

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

        // ✅ Upload file
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);
            return Ok(new { fileName = file.FileName, length = file.Length, url });
        }

        // ✅ List all files
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _storageService.ListFilesAsync();
            return Ok(files);
        }

        // ✅ Delete file
        [HttpDelete("{fileName}")]
        public async Task<IActionResult> Delete(string fileName)
        {
            await _storageService.DeleteAsync(fileName);
            return Ok(new { message = "File deleted successfully." });
        }
    }
}
