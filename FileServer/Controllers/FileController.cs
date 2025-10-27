using FileServer.Models;
using FileServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly FileStorageService _service;

        public FileController(FileStorageService service)
        {
            _service = service;
        }

        // ✅ Upload file (includes sender and receiver)
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _service.SaveFileAsync(request.File, request.Sender, request.Receiver);
            return Ok(result);
        }

        // ✅ List all files
        [HttpGet("list")]
        public IActionResult ListFiles()
        {
            var files = _service.ListFiles();
            return Ok(files);
        }

        // ✅ List files meant for a specific receiver
        [HttpGet("inbox/{receiver}")]
        public IActionResult GetInbox(string receiver)
        {
            var files = _service.ListFiles()
                .Where(f => f.ContainsKey("receiver") &&
                            string.Equals(f["receiver"]?.ToString(), receiver, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Ok(files);
        }

        // ✅ Download a file by token
        [HttpGet("download/{token}")]
        public IActionResult Download(string token)
        {
            var (path, name) = _service.GetFilePath(token);

            if (path == null || name == null)
                return NotFound("File not found.");

            var stream = System.IO.File.OpenRead(path);
            var contentType = "application/octet-stream";

            return File(stream, contentType, name);
        }
    }
}
