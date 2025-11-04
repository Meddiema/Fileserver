using FileServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FileServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly SupabaseStorageService _storageService;
        private readonly HttpClient _httpClient;

        public FileController()
        {
            _storageService = new SupabaseStorageService();
            _httpClient = new HttpClient();
        }

        // ✅ Upload file
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType);

            return Ok(new { file.FileName, file.Length, url });
        }

        // ✅ List all files
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _storageService.ListFilesAsync();
            return Ok(files);
        }

        // ✅ Download file via Supabase public URL
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Invalid file name.");

            var bucket = Environment.GetEnvironmentVariable("SUPABASE_BUCKET") ?? "upload";
            var baseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var fileUrl = $"{baseUrl}/storage/v1/object/public/{bucket}/{fileName}";

            try
            {
                var response = await _httpClient.GetAsync(fileUrl);
                if (!response.IsSuccessStatusCode)
                    return NotFound("File not found.");

                var stream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Download failed: {ex.Message}");
            }
        }

        // ✅ Delete file
        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            await _storageService.DeleteAsync(fileName);
            return Ok($"Deleted: {fileName}");
        }
    }
}
