using System.Text.Json.Serialization;

namespace NewWinampClassic.Models;

/// <summary>
/// Represents a SoundCloud search result from the oneportal.space proxy API.
/// JSON fields are PascalCase to match the API response.
/// </summary>
public class SoundCloudTrack
{
    [JsonPropertyName("Title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("Info2")]
    public string Info2 { get; set; } = string.Empty;

    /// <summary>
    /// SoundCloud track permalink URL (used to obtain stream link).
    /// </summary>
    [JsonPropertyName("Info3")]
    public string Info3 { get; set; } = string.Empty;

    [JsonPropertyName("Thumbnail")]
    public string Thumbnail { get; set; } = string.Empty;

    /// <summary>
    /// Artist / uploader name with like count (e.g. "Liam 👍 58,627").
    /// </summary>
    [JsonPropertyName("Info")]
    public string Info { get; set; } = string.Empty;

    [JsonPropertyName("Link")]
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Artist name cleaned from Info field.
    /// </summary>
    public string DisplayArtist
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Info))
                return "Unknown Artist";
            // Info format: "ArtistName 👍 58,627" — extract name before emoji
            var idx = Info.IndexOf('\uD83D');
            if (idx > 0)
                return Info[..idx].TrimEnd();
            return Info.Trim();
        }
    }

    /// <summary>
    /// Safe filename for saving (no special characters).
    /// </summary>
    public string SafeFileName => SanitizeFileName(Title);

    private static string SanitizeFileName(string name)
    {
        var invalid = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        foreach (var c in invalid)
            name = name.Replace(c, '_');
        return name.Trim();
    }
}
