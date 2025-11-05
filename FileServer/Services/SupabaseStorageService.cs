using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileServer.Services
{
    public class SupabaseStorageService
    {
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private readonly string _bucketName;
        private readonly HttpClient _httpClient;

        public SupabaseStorageService()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            _supabaseUrl = config["SUPABASE_URL"] ?? throw new InvalidOperationException("SUPABASE_URL is not set.");
            _supabaseKey = config["SUPABASE_KEY"] ?? throw new InvalidOperationException("SUPABASE_KEY is not set.");
            _bucketName = config["SUPABASE_BUCKET"] ?? "upload";

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        }

        // ✅ Upload file to Supabase public bucket
        public async Task<string> UploadAsync(Stream stream, string originalFileName, string contentType)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (string.IsNullOrEmpty(originalFileName))
                throw new ArgumentNullException(nameof(originalFileName));

            var path = $"{Guid.NewGuid()}_{originalFileName}";
            var uploadUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{path}";

            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");

            var response = await _httpClient.PostAsync(uploadUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase upload failed: {response.StatusCode} - {error}");
            }

            // Return public URL
            return $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{path}";
        }

        // ✅ List all files in Supabase bucket
        public async Task<List<(string Name, long Size, string PublicUrl)>> ListFilesAsync()
        {
            var listUrl = $"{_supabaseUrl}/storage/v1/object/list/{_bucketName}";

            var requestBody = new
            {
                prefix = "",
                limit = 100,
                offset = 0,
                sortBy = new { column = "created_at", order = "desc" }
            };

            var jsonBody = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(listUrl, jsonBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase list failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
            var result = new List<(string, long, string)>();

            if (files != null)
            {
                foreach (var file in files)
                {
                    string name = file.ContainsKey("name") ? file["name"]?.ToString() ?? "unknown" : "unknown";
                    long size = 0;

                    // Try extract size if metadata is present
                    if (file.ContainsKey("metadata") && file["metadata"] != null)
                    {
                        try
                        {
                            var metadata = file["metadata"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(metadata))
                            {
                                using var doc = JsonDocument.Parse(metadata);
                                if (doc.RootElement.TryGetProperty("size", out var sizeProp))
                                    size = sizeProp.GetInt64();
                            }
                        }
                        catch
                        {
                            size = 0; // fallback
                        }
                    }

                    string publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{name}";
                    result.Add((name, size, publicUrl));
                }
            }

            return result;
        }

        // ✅ Download file by name (legacy endpoint)
        public async Task<Stream> DownloadAsync(string fileName)
        {
            var downloadUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{fileName}";
            var response = await _httpClient.GetAsync(downloadUrl);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Download failed: {response.StatusCode}");

            return await response.Content.ReadAsStreamAsync();
        }

        // ✅ NEW — Download file directly from its full Supabase public URL
        public async Task<Stream> DownloadFromUrlAsync(string fileUrl)
        {
            var response = await _httpClient.GetAsync(fileUrl);
            if (!response.IsSuccessStatusCode)
                throw new FileNotFoundException($"Failed to download file from {fileUrl}");

            return await response.Content.ReadAsStreamAsync();
        }
    }
}
