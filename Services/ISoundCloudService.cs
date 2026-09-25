using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public interface ISoundCloudService
{
    Task<List<SoundCloudTrack>> SearchAsync(string keyword, int maxResults = 20);
    Task<string> GetStreamUrlAsync(SoundCloudTrack track);
    Task DownloadAsync(SoundCloudTrack track, string destinationFolder, IProgress<double>? progress = null);
}
