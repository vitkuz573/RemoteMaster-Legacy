// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IFileService
{
    string CalculateChecksum(string filePath);

    void DeleteFile(string filePath);

    void DeleteDirectory(string directoryPath, bool recursive = true);

    void CopyFile(string sourceFile, string destinationFile, bool overwrite = false);

    void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite = false);

    void CreateDirectory(string directoryPath);

    Task WaitForFileReleaseAsync(string directory, List<string>? excludedFolders = null);
}
