// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Dtos;

public class ChunkWrapper
{
    public byte[] Chunk { get; set; }

    public bool IsFirstChunk { get; set; }

    public bool IsLastChunk { get; set; }

    public int ChunkId { get; set; }

    public string SequenceId { get; set; }
}