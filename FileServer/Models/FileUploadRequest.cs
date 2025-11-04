using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileServer.Models
{
    public class FileUploadRequest
    {
        // ✅ Explicit form name so PowerShell, Postman, and Android all match
        [FromForm(Name = "file")]
        public required IFormFile File { get; set; }


        [FromForm(Name = "sender")]
        public string? Sender { get; set; }

        [FromForm(Name = "receiver")]
        public string? Receiver { get; set; }

    }
}
