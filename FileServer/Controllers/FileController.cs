using FileServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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

        // ✅ Upload a file to Supabase
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);

            return Ok(new
            {
                file.FileName,
                file.Length,
                url
            });
        }

        // ✅ List all files in the bucket
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _storageService.ListFilesAsync();
            return Ok(files);
        }

        // ✅ Delete file from Supabase bucket
        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Invalid file name.");

            await _storageService.DeleteAsync(fileName);
            return Ok($"File '{fileName}' deleted successfully.");
        }
    }
}
