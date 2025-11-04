using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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

        // ✅ Upload file to Supabase
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

            return $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{path}";
        }

        // ✅ List all files
        public async Task<List<string>> ListFilesAsync()
        {
            var listUrl = $"{_supabaseUrl}/storage/v1/object/list/{_bucketName}";

            var body = new
            {
                prefix = "",
                limit = 100,
                offset = 0,
                sortBy = new { column = "name", order = "asc" }
            };

            var jsonBody = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(listUrl, jsonBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase list failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
            var urls = new List<string>();

            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.TryGetValue("name", out var name))
                    {
                        urls.Add($"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{name}");
                    }
                }
            }

            return urls;
        }

        // ✅ Delete file
        public async Task DeleteAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Invalid file name.");

            var deleteUrl = $"{_supabaseUrl}/storage/v1/object/{_bucketName}/{fileName}";
            var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Supabase delete failed: {response.StatusCode} - {error}");
            }
        }

        // ✅ Optional - Download a file as stream (for mobile use)
        public async Task<Stream> DownloadAsync(string fileName)
        {
            var downloadUrl = $"{_supabaseUrl}/storage/v1/object/public/{_bucketName}/{fileName}";
            var response = await _httpClient.GetAsync(downloadUrl);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Download failed: {response.StatusCode}");

            return await response.Content.ReadAsStreamAsync();
        }
    }
}
