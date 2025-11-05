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

        // ✅ Upload file to Supabase bucket
        public async Task<string> UploadAsync(Stream stream, string originalFileName, string contentType)
        {
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

            return $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{path}";
        }

        // ✅ List all files with details
        public async Task<List<(string Name, long Size, string PublicUrl)>> ListFilesAsync()
        {
            var listUrl = $"{_supabaseUrl}/storage/v1/object/list/{_bucketName}";

            var body = new
            {
                prefix = "",
                limit = 100,
                offset = 0,
                sortBy = new { column = "name", order = "asc" }
            };

            var jsonBody = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(listUrl, jsonBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase list failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
            var result = new List<(string Name, long Size, string PublicUrl)>();

            if (files != null)
            {
                foreach (var file in files)
                {
                    string name = file.ContainsKey("name") ? file["name"]?.ToString() ?? "unknown" : "unknown";
                    long size = file.ContainsKey("metadata") && file["metadata"] != null
                        ? GetFileSize(file["metadata"].ToString())
                        : 0;

                    string publicUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{name}";
                    result.Add((name, size, publicUrl));
                }
            }

            return result;
        }

        private static long GetFileSize(string metadataJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(metadataJson);
                if (doc.RootElement.TryGetProperty("size", out var sizeProp))
                    return sizeProp.GetInt64();
            }
            catch { }
            return 0;
        }

        // ✅ Download file directly from Supabase public URL
        public async Task<Stream> DownloadFromUrlAsync(string fileUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, fileUrl);
            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new FileNotFoundException($"Failed to download file from {fileUrl} (Status: {response.StatusCode})");

            return await response.Content.ReadAsStreamAsync();
        }
    }
}
