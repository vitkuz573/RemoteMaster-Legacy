namespace RemoteMaster.Shared.Dto;

public class ChunkDto
{
    public byte[] Chunk { get; set; } // Чанк данных

    public bool IsFirstChunk { get; set; } // Является ли этот чанк первым в последовательности

    public bool IsLastChunk { get; set; } // Является ли этот чанк последним в последовательности

    public int ChunkId { get; set; } // Идентификатор чанка в последовательности

    public string InstanceId { get; set; }
}