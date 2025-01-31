// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Drawing;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Helpers.ScreenHelper;

public class Screen : IScreen
{
    public string DeviceName { get; }

    public Rectangle Bounds { get; }

    public bool Primary { get; }

    private Screen(string name, Size resolution, bool primary)
    {
        DeviceName = name;
        Bounds = new Rectangle(0, 0, resolution.Width, resolution.Height);
        Primary = primary;
    }

    public static IEnumerable<IScreen> AllScreens
    {
        get
        {
            var screens = new List<IScreen>();

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = "-c \"swaymsg -t get_outputs | jq -r '.[].name'\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            while (!process.StandardOutput.EndOfStream)
            {
                var displayName = process.StandardOutput.ReadLine()?.Trim();

                if (!string.IsNullOrEmpty(displayName))
                {
                    screens.Add(new Screen(displayName, GetScreenResolution(displayName), screens.Count == 0));
                }
            }

            process.WaitForExit();

            return screens;
        }
    }

    public static IScreen? PrimaryScreen => AllScreens.FirstOrDefault(s => s.Primary);

    private static Size GetScreenResolution(string displayName)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "sh",
            Arguments = $"-c \"swaymsg -t get_outputs | jq -r '.[] | select(.name == \"{displayName}\").current_mode'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        var resolution = process.StandardOutput.ReadLine()?.Trim();

        process.WaitForExit();

        if (string.IsNullOrEmpty(resolution) || !resolution.Contains("x"))
        {
            return new Size(1920, 1080);
        }

        var parts = resolution.Split('x');

        if (int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
        {
            return new Size(width, height);
        }

        return new Size(1920, 1080);
    }
}
