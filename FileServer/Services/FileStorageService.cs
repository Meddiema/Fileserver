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

        // ✅ Save file with both token and real filename, and store its full path in JSON
        public async Task<Dictionary<string, object>> SaveFileAsync(IFormFile file, string? sender, string? receiver)
        {
            var token = Guid.NewGuid().ToString("N");
            var originalName = file.FileName;

            // File will be saved with token + "_" + originalName
            var savedName = $"{token}_{originalName}";
            var fullPath = Path.Combine(_uploadDir, savedName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            var metadata = new
            {
                token,
                name = originalName,
                size = file.Length,
                uploaded = DateTime.UtcNow,
                sender,
                receiver,
                path = fullPath
            };

            var jsonPath = Path.Combine(_uploadDir, $"{token}.json");
            await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(metadata));

            return new Dictionary<string, object>
            {
                ["token"] = token,
                ["name"] = originalName,
                ["size"] = file.Length,
                ["sender"] = sender,
                ["receiver"] = receiver,
                ["path"] = fullPath,
                ["uploaded"] = DateTime.UtcNow
            };
        }

        // ✅ List files by reading JSON metadata
        public List<Dictionary<string, object>> ListFiles()
        {
            return Directory.GetFiles(_uploadDir, "*.json")
                .Select(file =>
                {
                    var json = File.ReadAllText(file);
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                })
                .Where(x => x != null)
                .ToList()!;
        }

        // ✅ Reliable GetFilePath — uses JSON metadata, not guessing
        public (string? Path, string? Name) GetFilePath(string token)
        {
            var metaFile = Path.Combine(_uploadDir, $"{token}.json");
            if (!File.Exists(metaFile))
                return (null, null);

            try
            {
                var metaJson = File.ReadAllText(metaFile);
                var meta = JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson);

                if (meta == null || !meta.ContainsKey("path") || !meta.ContainsKey("name"))
                    return (null, null);

                var fullPath = meta["path"]?.ToString();
                var name = meta["name"]?.ToString();

                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                    return (null, null);

                return (fullPath, name);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
