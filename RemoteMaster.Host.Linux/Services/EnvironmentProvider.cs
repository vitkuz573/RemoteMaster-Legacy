// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using FluentResults;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class EnvironmentProvider(ILogger<EnvironmentProvider> logger) : IEnvironmentProvider
{
    public string GetDisplay()
    {
        return ":0";
    }

    public string GetXAuthority()
    {
        try
        {
            // Try to obtain the X authority information from the Xorg process.
            var xAuthResult = TryGetXAuth("Xorg");

            if (xAuthResult.IsSuccess)
            {
                logger.LogInformation("Found '-auth' parameter with value: {XAuthority} on display: {XDisplay}",
                    xAuthResult.Value.XAuthority, xAuthResult.Value.XDisplay);
                return xAuthResult.Value.XAuthority;
            }
            else
            {
                logger.LogError("Failed to retrieve XAuthority: {Errors}",
                    string.Join(", ", xAuthResult.Errors.Select(e => e.Message)));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred while retrieving the XAuthority.");
        }

        return string.Empty;
    }

    /// <summary>
    /// Tries to retrieve the X display and X authority information from the specified X server process.
    /// </summary>
    /// <param name="xServerProcess">The X server process name (e.g. "Xorg").</param>
    /// <returns>A FluentResults Result containing an XAuthInfo record on success, or errors on failure.</returns>
    private Result<XAuthInfo> TryGetXAuth(string xServerProcess)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = $"-C {xServerProcess} -f",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            logger.LogDebug("Starting process: {FileName} {Arguments}", psi.FileName, psi.Arguments);

            using var process = Process.Start(psi);
            if (process is null)
            {
                return Result.Fail<XAuthInfo>($"Failed to start process for {xServerProcess}.");
            }

            // Read the entire output safely.
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            logger.LogDebug("{XServerProcess} process output: {Output}", xServerProcess, output);

            // Look for a line that contains "-auth".
            var line = output.Split(Environment.NewLine)
                             .FirstOrDefault(l => l.Contains(" -auth "));

            if (string.IsNullOrWhiteSpace(line))
            {
                return Result.Fail<XAuthInfo>($"No line containing '-auth' found for {xServerProcess}.");
            }

            logger.LogInformation("Resolved {XServerProcess} process line: {Line}", xServerProcess, line);

            // Split the line into tokens.
            var tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var xdisplay = ":0";
            var xauthority = string.Empty;

            // Optionally, try to detect the DISPLAY token (e.g. ":0").
            var displayToken = tokens.FirstOrDefault(t => t.StartsWith(":"));
            if (!string.IsNullOrWhiteSpace(displayToken))
            {
                xdisplay = displayToken;
            }

            // Find the "-auth" parameter and its following value.
            var authIndex = Array.IndexOf(tokens, "-auth");
            if (authIndex >= 0 && authIndex < tokens.Length - 1)
            {
                xauthority = tokens[authIndex + 1];
            }
            else
            {
                return Result.Fail<XAuthInfo>($"'-auth' parameter not found in {xServerProcess} process arguments.");
            }

            var whoOutput = GetWhoOutput();

            if (!string.IsNullOrWhiteSpace(whoOutput))
            {
                try
                {
                    var whoLine = whoOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                             .FirstOrDefault(l => l.Contains(xdisplay));
                    if (!string.IsNullOrWhiteSpace(whoLine))
                    {
                        var whoTokens = whoLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        if (whoTokens.Length > 0)
                        {
                            var username = whoTokens[0];
                            var userXAuthority = $"/home/{username}/.Xauthority";
                            logger.LogInformation("Detected user {Username} from 'who' command. Using XAUTHORITY: {UserXAuthority}", username, userXAuthority);
                            xauthority = userXAuthority;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while parsing 'who' command output.");
                }
            }

            return Result.Ok(new XAuthInfo(xdisplay, xauthority));
        }
        catch (Exception ex)
        {
            return Result.Fail<XAuthInfo>(ex.Message);
        }
    }

    /// <summary>
    /// Executes the "who" command to retrieve current user session information.
    /// </summary>
    /// <returns>The output of the "who" command, or an empty string if an error occurs.</returns>
    private string GetWhoOutput()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "who",
                Arguments = "",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            
            if (process is null)
            {
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            
            process.WaitForExit();
            
            return output;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while running 'who' command.");
            return string.Empty;
        }
    }

    private record XAuthInfo(string XDisplay, string XAuthority);
}
