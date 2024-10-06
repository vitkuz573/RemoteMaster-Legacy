// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class FileUploadDto(string name, byte[] data, string destinationPath)
{
    public string Name { get; } = name;

    public byte[] Data { get; } = data;

    public string DestinationPath { get; } = destinationPath;
}
