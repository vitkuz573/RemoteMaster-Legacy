// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IHelpService
{
    void PrintHelp(string? modeName = null);

    void PrintMissingParametersError(string modeName, IEnumerable<KeyValuePair<string, ILaunchParameter>> missingParameters);

    void SuggestSimilarModes(string inputMode);
}
