using System.Diagnostics;
using FluentResults;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class EnvironmentProvider(ILogger<EnvironmentProvider> logger) : IEnvironmentProvider
{
    public string GetDisplay()
    {
        var result = TryGetXAuth("Xorg");
        
        if (result.IsSuccess)
        {
            logger.LogInformation("Found display: {XDisplay}", result.Value.XDisplay);

            return result.Value.XDisplay;
        }

        logger.LogWarning("Failed to get display, using fallback \":0\". Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        
        return ":0";
    }

    public string GetXAuthority()
    {
        var result = TryGetXAuth("Xorg");
        
        if (result.IsSuccess)
        {
            logger.LogInformation("Found '-auth' parameter with value: {XAuthority} on display: {XDisplay}", result.Value.XAuthority, result.Value.XDisplay);
            
            return result.Value.XAuthority;
        }

        logger.LogError("Failed to retrieve XAuthority: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
        
        return string.Empty;
    }

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

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            var xProcess = lines.FirstOrDefault(line => line.Contains(" -auth "));
            
            if (string.IsNullOrWhiteSpace(xProcess))
            {
                logger.LogInformation("{xServerProcess} process not found.", xServerProcess);
               
                return Result.Fail<XAuthInfo>($"{xServerProcess} process not found.");
            }

            logger.LogInformation("Resolved X server process: {xProcess}", xProcess);

            var tokens = xProcess.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            var xDisplay = ":0";

            var xProcIndex = tokens.FindIndex(t => t.EndsWith(xServerProcess, StringComparison.OrdinalIgnoreCase));
            
            if (xProcIndex >= 0 && tokens.Count > xProcIndex + 1 && tokens[xProcIndex + 1].StartsWith(":"))
            {
                xDisplay = tokens[xProcIndex + 1];
            }

            string xAuthority;

            var authIndex = tokens.FindIndex(t => t.Equals("-auth", StringComparison.OrdinalIgnoreCase));
            
            if (authIndex >= 0 && tokens.Count > authIndex + 1)
            {
                xAuthority = tokens[authIndex + 1];
            }
            else
            {
                return Result.Fail<XAuthInfo>($"'-auth' parameter not found in {xServerProcess} process arguments.");
            }

            return Result.Ok(new XAuthInfo(xDisplay, xAuthority));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while getting X auth for {xServerProcess}.", xServerProcess);
            
            return Result.Fail<XAuthInfo>($"Error while getting X auth for {xServerProcess}: {ex.Message}");
        }
    }

    private record XAuthInfo(string XDisplay, string XAuthority);
}
