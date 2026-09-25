using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public class SoundCloudService : ISoundCloudService
{
    private const string ApiBase = "https://us.oneportal.space/api/";
    private const string EncryptionKey = "g09uj30fgjiodfjg09j";
    private static readonly HttpClient _httpClient = new();
    private readonly ILogger<SoundCloudService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SoundCloudService(ILogger<SoundCloudService> logger)
    {
        _logger = logger;
    }

    public async Task<List<SoundCloudTrack>> SearchAsync(string keyword, int maxResults = 20)
    {
        try
        {
            var url = $"{ApiBase}sc/Search?keyword={Uri.EscapeDataString(keyword)}";
            var response = await _httpClient.GetStringAsync(url);

            var tracks = JsonSerializer.Deserialize<List<SoundCloudTrack>>(response, _jsonOptions);
            if (tracks == null || tracks.Count == 0)
                return [];

            return tracks.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search SoundCloud for '{Keyword}'", keyword);
            return [];
        }
    }

    public async Task<string> GetStreamUrlAsync(SoundCloudTrack track)
    {
        var playLinkUrl = $"{ApiBase}sc/GetPlayLink?link={Uri.EscapeDataString(track.Info3)}";
        var playLinkResponse = await _httpClient.GetStringAsync(playLinkUrl);
        var encryptedUrl = playLinkResponse.Trim();

        if (string.IsNullOrWhiteSpace(encryptedUrl))
            throw new InvalidOperationException("Failed to get stream URL from API.");

        return Decrypt(encryptedUrl);
    }

    public async Task DownloadAsync(SoundCloudTrack track, string destinationFolder, IProgress<double>? progress = null)
    {
        try
        {
            // Step 1: Get the play/stream link
            progress?.Report(10);
            var streamUrl = await GetStreamUrlAsync(track);

            // Step 2: Download the audio file
            progress?.Report(30);

            using var httpResponse = await _httpClient.GetAsync(streamUrl, HttpCompletionOption.ResponseHeadersRead);
            httpResponse.EnsureSuccessStatusCode();

            var totalBytes = httpResponse.Content.Headers.ContentLength ?? -1L;
            var filePath = Path.Combine(destinationFolder, $"{track.SafeFileName}.mp3");

            await using var contentStream = await httpResponse.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);
            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var downloadProgress = 30 + (totalRead / (double)totalBytes) * 60;
                    progress?.Report(downloadProgress);
                }
            }

            progress?.Report(100);
            _logger.LogInformation("Downloaded '{Title}' to {FilePath}", track.Title, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download '{Title}'", track.Title);
            throw;
        }
    }

    /// <summary>
    /// AES decryption matching the server-side StringCipher.Encrypt.
    /// </summary>
    private static string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return string.Empty;

        cipherText = cipherText.Replace(" ", "+");
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var encryptor = Aes.Create();
        var pdb = new Rfc2898DeriveBytes(EncryptionKey,
            [0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76]);
        encryptor.Key = pdb.GetBytes(32);
        encryptor.IV = pdb.GetBytes(16);

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(cipherBytes, 0, cipherBytes.Length);
        cs.Close();

        return Encoding.Unicode.GetString(ms.ToArray());
    }
}
