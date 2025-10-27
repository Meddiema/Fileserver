namespace FileServer.Models
{
    public class FileMetadata
    {
        public string Token { get; set; } = default!;
        public string Name { get; set; } = default!;
        public long Size { get; set; }
        public string Path { get; set; } = default!;
        public DateTime Uploaded { get; set; }
    }
}
