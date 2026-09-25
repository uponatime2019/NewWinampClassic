using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace NewWinampClassic.Helpers;

public static class ArtworkCache
{
    public static string GetCacheDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NewWinampClassic",
            "Artwork");
    }

    public static void EnsureCacheDirectory()
    {
        var directory = GetCacheDirectory();
        Directory.CreateDirectory(directory);
    }

    public static string? GetCachedArtworkPath(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath))
            return null;

        var hash = ComputeHash(sourcePath);
        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrEmpty(extension))
            extension = ".jpg";

        var cacheDirectory = GetCacheDirectory();
        return Path.Combine(cacheDirectory, $"{hash}{extension}");
    }

    public static async Task<string?> CacheArtworkAsync(string sourcePath, byte[] imageData)
    {
        if (string.IsNullOrEmpty(sourcePath) || imageData is null || imageData.Length == 0)
            return null;

        var cachePath = GetCachedArtworkPath(sourcePath);
        if (cachePath is null)
            return null;

        EnsureCacheDirectory();

        await File.WriteAllBytesAsync(cachePath, imageData).ConfigureAwait(false);
        return cachePath;
    }

    public static byte[]? GetCachedArtwork(string cachePath)
    {
        if (string.IsNullOrEmpty(cachePath) || !File.Exists(cachePath))
            return null;

        return File.ReadAllBytes(cachePath);
    }

    public static void ClearCache()
    {
        var directory = GetCacheDirectory();
        if (!Directory.Exists(directory))
            return;

        foreach (var file in Directory.GetFiles(directory))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // File may be in use; skip it
            }
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
