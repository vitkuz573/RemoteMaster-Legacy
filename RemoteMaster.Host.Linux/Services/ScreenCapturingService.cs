// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Helpers.ScreenHelper;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Linux.Services;

public class ScreenCapturingService : IScreenCapturingService
{
    public byte[]? GetNextFrame(string connectionId)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = "-c \"xwd -root -silent | ffmpeg -f xwd -i - -vf format=rgb24 -vframes 1 -f image2pipe -vcodec png -\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            using var memoryStream = new MemoryStream();

            process.StandardOutput.BaseStream.CopyTo(memoryStream);
            process.WaitForExit();

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to capture screen frame", ex);
        }
    }

    public IEnumerable<Display> GetDisplays()
    {
        return Screen.AllScreens.Select(s => new Display
        {
            Name = s.DeviceName,
            IsPrimary = s.Primary,
            Resolution = s.Bounds.Size
        });
    }

    public IScreen? FindScreenByName(string displayName)
    {
        return Screen.AllScreens.FirstOrDefault(s => s.DeviceName == displayName);
    }

    public void SetSelectedScreen(string connectionId, IScreen display) { }

    public byte[]? GetThumbnail(string connectionId) => GetNextFrame(connectionId);

    public void Dispose() { }
}
