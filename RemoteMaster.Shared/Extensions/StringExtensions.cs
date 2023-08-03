// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Extensions;

public static class StringExtensions
{
    private static string? SanitizeString(string? input, char[] invalidChars)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var invalidCharsSet = new HashSet<char>(invalidChars);

        return new string(input?.Where(c => !invalidCharsSet.Contains(c)).ToArray());
    }

    public static string? SanitizeFileName(this string? fileName) => SanitizeString(fileName, Path.GetInvalidFileNameChars());

    public static string? SanitizeFileSystemPath(this string? path) => SanitizeString(path, Path.GetInvalidPathChars());
}