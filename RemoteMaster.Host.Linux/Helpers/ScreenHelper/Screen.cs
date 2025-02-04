// Copyright © 2023 Vitaly Kuzyaev.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Drawing;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Helpers.ScreenHelper;

/// <summary>
/// Represents a display.
/// This implementation uses xrandr to retrieve display information,
/// making it universal for most X11 environments (e.g. GNOME).
/// </summary>
public class Screen : IScreen
{
    public string DeviceName { get; }
    
    public Rectangle Bounds { get; }
    
    public bool Primary { get; }

    public Screen(string name, Size resolution, bool primary)
    {
        DeviceName = name;
        Bounds = new Rectangle(0, 0, resolution.Width, resolution.Height);
        Primary = primary;
    }

    /// <summary>
    /// Retrieves all connected screens by parsing the xrandr output.
    /// </summary>
    public static IEnumerable<IScreen> AllScreens
    {
        get
        {
            var screens = new List<IScreen>();

            try
            {
                var output = RunCommand("xrandr --query");

                if (string.IsNullOrWhiteSpace(output))
                {
                    return screens;
                }

                // Example xrandr output line:
                // "Virtual1 connected primary 1918x920+0+0 (normal left inverted right x axis y axis) 0mm x 0mm"
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains(" connected "))
                    {
                        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        if (tokens.Length < 3)
                        {
                            continue;
                        }

                        // The first token is the display name.
                        var name = tokens[0];

                        // Look for a token that contains resolution info (e.g. "1918x920+0+0")
                        foreach (var token in tokens)
                        {
                            // Check for the pattern: "WIDTHxHEIGHT+..."
                            if (token.Contains("x") && token.Contains("+"))
                            {
                                var resPart = token.Split('+')[0]; // "1918x920"
                                var dims = resPart.Split('x');
                                if (dims.Length == 2 &&
                                    int.TryParse(dims[0], out int width) &&
                                    int.TryParse(dims[1], out int height))
                                {
                                    // Determine if this display is primary.
                                    bool primary = line.Contains(" primary ");
                                    screens.Add(new Screen(name, new Size(width, height), primary));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Screen] Failed to retrieve screens: {ex.Message}");
            }

            return screens;
        }
    }

    /// <summary>
    /// Retrieves the primary screen.
    /// </summary>
    public static IScreen? PrimaryScreen => AllScreens.FirstOrDefault(s => s.Primary);

    private static string RunCommand(string command)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "sh",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return result.Trim();
    }
}
