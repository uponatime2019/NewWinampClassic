using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NewWinampClassic.Helpers;

public static class FolderUtility
{
    /// <summary>
    /// Checks if a candidate directory is a subdirectory of a specified parent directory.
    /// </summary>
    public static bool IsSubDirectoryOf(string candidate, string parent)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(parent))
            return false;

        try
        {
            var c = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var p = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return c.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || 
                   c.StartsWith(p + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes a collection of directory paths by removing duplicates and auto-skipping
    /// any subdirectory if its parent directory is already present in the list.
    /// </summary>
    public static List<string> NormalizeFolders(IEnumerable<string> folders)
    {
        if (folders is null)
            return [];

        var uniqueFolders = folders
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f =>
            {
                try
                {
                    return Path.GetFullPath(f);
                }
                catch
                {
                    return f;
                }
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f.Length) // Shorter paths (parent directories) are processed first
            .ToList();

        var normalized = new List<string>();
        foreach (var folder in uniqueFolders)
        {
            if (normalized.Any(p => IsSubDirectoryOf(folder, p) || folder.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                continue; // Auto-skip child folder or exact duplicate
            }
            normalized.Add(folder);
        }

        return normalized;
    }
}
