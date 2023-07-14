using MessagePack;
using Microsoft.Extensions.Caching.Memory;
using RemoteMaster.Shared.Dto;

namespace RemoteMaster.Shared.Helpers;

public static class Chunker
{
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public static IEnumerable<ChunkDto> Chunkify<T>(T data, int chunkSize = 4096) where T : class
    {
        var serializedData = MessagePackSerializer.Serialize(data);

        return GenerateChunks(serializedData, chunkSize);
    }

    public static IEnumerable<ChunkDto> Chunkify(byte[] data, int chunkSize = 4096)
    {
        return GenerateChunks(data, chunkSize);
    }

    private static IEnumerable<ChunkDto> GenerateChunks(byte[] data, int chunkSize)
    {
        var chunkCount = (int)Math.Ceiling((double)data.Length / chunkSize);
        var instanceId = Guid.NewGuid().ToString();

        for (var i = 0; i < chunkCount; i++)
        {
            var chunk = new byte[chunkSize];
            var index = 0;

            for (var j = i * chunkSize; j < (i + 1) * chunkSize; j++)
            {
                if (j < data.Length)
                {
                    chunk[index] = data[j];
                    index++;
                }
                else
                {
                    break;
                }
            }

            yield return new ChunkDto
            {
                Chunk = chunk,
                IsFirstChunk = i == 0,
                IsLastChunk = i == chunkCount - 1,
                ChunkId = i,
                InstanceId = instanceId
            };
        }
    }

    public static bool TryUnchunkify<T>(ChunkDto chunkDto, out T result) where T : class
    {
        var chunks = AddToCache(chunkDto);

        if (!chunkDto.IsLastChunk)
        {
            result = default;

            return false;
        }

        var allBytes = CombineChunks(chunks);

        result = MessagePackSerializer.Deserialize<T>(allBytes);

        return true;
    }

    public static bool TryUnchunkify(ChunkDto chunkDto, out byte[] result)
    {
        var chunks = AddToCache(chunkDto);

        if (!chunkDto.IsLastChunk)
        {
            result = default;

            return false;
        }

        result = CombineChunks(chunks);

        return true;
    }

    private static List<ChunkDto> AddToCache(ChunkDto chunkDto)
    {
        var chunks = _cache.GetOrCreate(chunkDto.InstanceId, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(1);

            return new List<ChunkDto>();
        });

        lock (chunks)
        {
            chunks.Add(chunkDto);
        }

        if (chunkDto.IsLastChunk)
        {
            _cache.Remove(chunkDto.InstanceId);
        }

        return chunks;
    }

    private static byte[] CombineChunks(List<ChunkDto> chunks)
    {
        var allBytes = new List<byte>();

        foreach (var chunk in chunks.OrderBy(c => c.ChunkId))
        {
            allBytes.AddRange(chunk.Chunk);
        }

        return allBytes.ToArray();
    }
}
