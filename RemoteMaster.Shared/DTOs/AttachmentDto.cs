// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class AttachmentDto
{
    public string FileName { get; set; }

    public byte[] Data { get; set; }

    public string MimeType { get; set; }
}