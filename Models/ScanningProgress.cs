namespace NewWinampClassic.Models;

public record ScanningProgress(int TotalFiles, int ProcessedFiles, string CurrentFile, bool IsComplete);
