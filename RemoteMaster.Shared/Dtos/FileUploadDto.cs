// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Dtos;

public class FileUploadDto
{
    public string Name { get; init; }

    public byte[] Data { get; init; }

    public string DestinationPath { get; init; }
}
