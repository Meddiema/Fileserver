using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileServer.Models
{
    public class FileUploadRequest
    {
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }

        [FromForm(Name = "sender")]
        public string? Sender { get; set; }

        [FromForm(Name = "receiver")]
        public string? Receiver { get; set; }
    }
}
