// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Helpers;

namespace RemoteMaster.Host.Core.Services;

public class ArgumentParser(IArgumentSerializer serializer, IHelpService helpService) : IArgumentParser
{
    public LaunchModeBase? ParseArguments(string[] args)
    {
        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            var modeName = ArgumentUtils.ExtractLaunchModeName(args);
            helpService.PrintHelp(modeName);

            return null;
        }

        try
        {
            return serializer.Deserialize(args);
        }
        catch (ArgumentException)
        {
            helpService.PrintHelp(null);
            throw;
        }
    }
}
