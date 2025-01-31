// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
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

            try
            {
                var output = RunCommand("swaymsg -t get_outputs");
                
                if (string.IsNullOrEmpty(output))
                {
                    return screens;
                }

                using var jsonDoc = JsonDocument.Parse(output);
                
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    var name = element.GetProperty("name").GetString();
                    var primary = element.GetProperty("focused").GetBoolean();
                    var resolution = GetScreenResolution(element);

                    if (!string.IsNullOrEmpty(name))
                    {
                        screens.Add(new Screen(name, resolution, primary));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get screens: {ex.Message}");
            }

            return screens;
        }
    }

    public static IScreen? PrimaryScreen => AllScreens.FirstOrDefault(s => s.Primary);

    private static Size GetScreenResolution(JsonElement screenElement)
    {
        try
        {
            var width = screenElement.GetProperty("rect").GetProperty("width").GetInt32();
            var height = screenElement.GetProperty("rect").GetProperty("height").GetInt32();
            
            return new Size(width, height);
        }
        catch
        {
            return new Size(1920, 1080);
        }
    }

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
