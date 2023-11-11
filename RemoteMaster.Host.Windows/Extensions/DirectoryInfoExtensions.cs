// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Extensions;

public static class DirectoryInfoExtensions
{
    public static void DeepCopy(this DirectoryInfo directory, string destinationDir, bool overwriteExisting = false)
    {
        if (directory == null)
        {
            throw new ArgumentNullException(nameof(directory));
        }

        foreach (var dir in Directory.GetDirectories(directory.FullName, "*", SearchOption.AllDirectories))
        {
            var dirToCreate = dir.Replace(directory.FullName, destinationDir);
            Directory.CreateDirectory(dirToCreate);
        }

        foreach (var file in Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories))
        {
            var destFile = file.Replace(directory.FullName, destinationDir);

            if (!File.Exists(destFile) || overwriteExisting)
            {
                File.Copy(file, destFile, true);
            }
        }
    }
}
