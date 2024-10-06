// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class AttachmentDto(string fileName, byte[] data, string mimeType)
{
    public string FileName { get; } = fileName;

    public byte[] Data { get; } = data;

    public string MimeType { get; } = mimeType;
}
