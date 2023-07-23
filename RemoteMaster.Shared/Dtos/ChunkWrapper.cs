namespace RemoteMaster.Shared.Dtos;

public class ChunkWrapper
{
    public byte[] Chunk { get; set; }

    public bool IsFirstChunk { get; set; }

    public bool IsLastChunk { get; set; }

    public int ChunkId { get; set; }

    public string SequenceId { get; set; }
}