using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FileServer.Services
{
    public class SupabaseStorageService
    {
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private readonly string _bucket;

        public SupabaseStorageService()
        {
            _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ??
                           throw new InvalidOperationException("SUPABASE_URL is not set.");
            _supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY") ??
                           throw new InvalidOperationException("SUPABASE_KEY is not set.");
            _bucket = Environment.GetEnvironmentVariable("SUPABASE_BUCKET") ?? "upload";
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient();
            // Supabase expects both apikey and Authorization header for server requests
            client.DefaultRequestHeaders.Remove("apikey");
            client.DefaultRequestHeaders.Add("apikey", _supabaseKey);

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");

            return client;
        }

        /// <summary>
        /// Uploads the provided stream to Supabase Storage and returns the public URL.
        /// </summary>
        public async Task<string> UploadAsync(Stream stream, string originalFileName, string contentType)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentNullException(nameof(originalFileName));

            // Read bytes (Supabase accepts raw body)
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var token = Guid.NewGuid().ToString("N");
            var safeFileName = Path.GetFileName(originalFileName);
            var objectPath = $"{token}_{safeFileName}";
            var encodedPath = Uri.EscapeDataString(objectPath);

            var uploadUrl = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/{_bucket}/{encodedPath}";

            using var client = CreateClient();
            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentLength = bytes.Length;
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType);

            var resp = await client.PutAsync(uploadUrl, content);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Supabase upload failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}");
            }

            // Construct public URL
            var publicUrl = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{_bucket}/{encodedPath}";
            return publicUrl;
        }

        /// <summary>
        /// Lists objects in the bucket. Returns a simple list of file metadata objects:
        /// { name, size, updated_at }
        /// </summary>
        public async Task<List<object>> ListFilesAsync()
        {
            var listUrl = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/list/{_bucket}";
            using var client = CreateClient();

            // Many Supabase setups accept POST to list; try POST then fallback to GET
            HttpResponseMessage resp = null;
            try
            {
                resp = await client.PostAsync(listUrl, null);
            }
            catch
            {
                // ignore and try GET below
            }

            if (resp == null || !resp.IsSuccessStatusCode)
            {
                // try GET (some Supabase versions require GET with ?prefix=)
                var getUrl = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/list/{_bucket}";
                resp = await client.GetAsync(getUrl);
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Supabase list failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}");
            }

            var respBody = await resp.Content.ReadAsStringAsync();
            // parse as JSON array of objects
            try
            {
                var doc = JsonDocument.Parse(respBody);
                var arr = new List<object>();

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        // build a small object with the fields we care about
                        string name = el.TryGetProperty("name", out var pn) ? pn.GetString() :
                                      el.TryGetProperty("id", out var pi) ? pi.GetString() : null;
                        long size = el.TryGetProperty("size", out var ps) && ps.TryGetInt64(out var s) ? s : 0;
                        string updatedAt = el.TryGetProperty("updated_at", out var pu) ? pu.GetString() : null;

                        if (!string.IsNullOrEmpty(name))
                        {
                            arr.Add(new
                            {
                                name,
                                size,
                                updated = updatedAt,
                                url = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{_bucket}/{Uri.EscapeDataString(name)}"
                            });
                        }
                    }
                }

                return arr;
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse Supabase list response: " + ex.Message);
            }
        }

        /// <summary>
        /// Delete object at given objectPath (exact name, e.g. "token_filename.ext")
        /// </summary>
        public async Task DeleteAsync(string objectPath)
        {
            if (string.IsNullOrWhiteSpace(objectPath)) throw new ArgumentNullException(nameof(objectPath));

            var encodedPath = Uri.EscapeDataString(objectPath);
            var deleteUrl = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/{_bucket}/{encodedPath}";

            using var client = CreateClient();
            var resp = await client.DeleteAsync(deleteUrl);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Supabase delete failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}");
            }
        }

        /// <summary>
        /// Helper to get public URL for a stored object name (token_filename.ext)
        /// </summary>
        public string GetPublicUrl(string objectPath)
        {
            if (string.IsNullOrWhiteSpace(objectPath)) return string.Empty;
            return $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{_bucket}/{Uri.EscapeDataString(objectPath)}";
        }
    }
}
