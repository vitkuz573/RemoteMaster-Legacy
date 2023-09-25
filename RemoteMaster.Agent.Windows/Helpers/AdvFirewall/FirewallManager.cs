// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Agent.Helpers.AdvFirewall;

public static class FirewallManager
{
    public static void EnableWinRM()
    {
        ExecuteCommand("winrm", "qc -force");
    }

    public static void AddRule(FirewallRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        var profileStr = string.Join(",", rule.Profiles.Select(p => p.ToString().ToLower()));
        var cmdArgs = $"AdvFirewall firewall add rule name=\"{rule.Name}\" dir={rule.Direction.ToString().ToLower()} action={rule.Action.ToString().ToLower()} protocol={rule.Protocol.ToString()} localport={rule.LocalPort} profile={profileStr} program={rule.Program} service={rule.Service}";
        ExecuteCommand("netsh", cmdArgs);
    }

    public static void DeleteRule(string ruleName, RuleDirection direction)
    {
        var cmdArgs = $"AdvFirewall firewall delete rule name=\\\"{ruleName}\\\" dir={direction.ToString().ToLower()}";
        ExecuteCommand("netsh", cmdArgs);
    }

    public static string ShowRuleDetails(string ruleName)
    {
        var cmdArgs = $"AdvFirewall firewall show rule name=\"{ruleName}\"";

        return ExecuteCommandWithOutput("netsh", cmdArgs);
    }

    public static void SetRuleGroup(string groupName, RuleGroupStatus status)
    {
        var statusStr = status == RuleGroupStatus.Enabled ? "true" : "false";
        var cmdArgs = $"AdvFirewall firewall set rule group=\"{groupName}\" new enable={statusStr}";
        ExecuteCommand("netsh", cmdArgs);
    }

    private static void ExecuteCommand(string cmd, string args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
    }

    private static string ExecuteCommandWithOutput(string cmd, string args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return result;
    }
}
