using System.Text.Json;

namespace FileServer.Services
{
    public class FileStorageService
    {
        private readonly string _uploadDir;

        public FileStorageService()
        {
            _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(_uploadDir);
        }

        public async Task<Dictionary<string, object>> SaveFileAsync(IFormFile file, string? sender, string? receiver)
        {
            var token = Guid.NewGuid().ToString("N");
            var savedName = $"{token}_{file.FileName}";
            var path = Path.Combine(_uploadDir, savedName);

            await using (var fs = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            var metadata = new
            {
                token,
                name = file.FileName,
                size = file.Length,
                uploaded = DateTime.UtcNow,
                sender,
                receiver
            };

            var jsonPath = Path.Combine(_uploadDir, $"{token}.json");
            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(metadata));

            return new Dictionary<string, object>
            {
                ["token"] = token,
                ["name"] = file.FileName,
                ["size"] = file.Length,
                ["sender"] = sender,
                ["receiver"] = receiver
            };
        }

        public List<Dictionary<string, object>> ListFiles()
        {
            return Directory.GetFiles(_uploadDir, "*.json")
                .Select(f =>
                {
                    var json = File.ReadAllText(f);
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                })
                .Where(x => x != null)
                .ToList()!;
        }

        public (string? Path, string? Name) GetFilePath(string token)
        {
            var metaFile = Path.Combine(_uploadDir, $"{token}.json");
            if (!File.Exists(metaFile))
                return (null, null);

            var fileName = Directory.GetFiles(_uploadDir)
                .FirstOrDefault(f => Path.GetFileName(f).StartsWith(token) && !f.EndsWith(".json"));

            if (fileName == null)
                return (null, null);

            var name = Path.GetFileName(fileName).Substring(token.Length + 1);
            return (fileName, name);
        }
    }
}
