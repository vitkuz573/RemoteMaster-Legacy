// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ChecksumValidator(IFileService fileService) : IChecksumValidator
{
    public bool AreChecksumsEqual(string firstFile, string secondFile)
    {
        var firstChecksum = fileService.CalculateChecksum(firstFile);
        var secondChecksum = fileService.CalculateChecksum(secondFile);

        return firstChecksum == secondChecksum;
    }
}
