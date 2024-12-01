// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Helpers;

public static class ArgumentUtils
{
    public static string? ExtractLaunchModeName(string[] args)
    {
        var modeArg = args.FirstOrDefault(arg => arg.StartsWith("--launch-mode=", StringComparison.OrdinalIgnoreCase));
       
        if (modeArg == null)
        {
            return null;
        }

        var equalsIndex = modeArg.IndexOf('=');

        return equalsIndex == -1 || equalsIndex == modeArg.Length - 1 ? null : modeArg[(equalsIndex + 1)..].Trim();
    }
}
