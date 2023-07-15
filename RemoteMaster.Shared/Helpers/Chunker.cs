using MessagePack;
using Microsoft.Extensions.Caching.Memory;
using RemoteMaster.Shared.Dto;
using System.Collections.Concurrent;

namespace RemoteMaster.Shared.Helpers;

public static class Chunker
{
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public static IEnumerable<ChunkDto> Chunkify<T>(T data, int chunkSize = 4096, MessagePackSerializerOptions options = null) where T : class
    {
        if (data == null)
        {
            throw new InvalidOperationException("Data cannot be null");
        }

        byte[] serializedData;

        if (data is string str && string.IsNullOrEmpty(str))
        {
            yield return new ChunkDto
            {
                Chunk = Array.Empty<byte>(),
                IsFirstChunk = true,
                IsLastChunk = true,
                ChunkId = 0,
                SequenceId = Guid.NewGuid().ToString()
            };

            yield break;
        }

        try
        {
            serializedData = options != null ?
                MessagePackSerializer.Serialize(data, options) :
                MessagePackSerializer.Serialize(data);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to serialize data", ex);
        }

        if (serializedData.Length == 0)
        {
            yield break;
        }

        foreach (var chunk in GenerateChunks(serializedData, chunkSize))
        {
            yield return chunk;
        }
    }

    public static IEnumerable<ChunkDto> Chunkify(byte[] data, int chunkSize = 4096)
    {
        return GenerateChunks(data, chunkSize);
    }

    private static IEnumerable<ChunkDto> GenerateChunks(byte[] data, int chunkSize)
    {
        var chunkCount = (int)Math.Ceiling((double)data.Length / chunkSize);
        var sequenceId = Guid.NewGuid().ToString();

        for (var i = 0; i < chunkCount; i++)
        {
            var length = i < chunkCount - 1 ? chunkSize : data.Length - i * chunkSize;
            var chunk = new byte[length];

            Array.Copy(data, i * chunkSize, chunk, 0, length);

            yield return new ChunkDto
            {
                Chunk = chunk,
                IsFirstChunk = i == 0,
                IsLastChunk = i == chunkCount - 1,
                ChunkId = i,
                SequenceId = sequenceId
            };
        }
    }

    public static bool TryUnchunkify<T>(ChunkDto chunkDto, out T result, MessagePackSerializerOptions options = null) where T : class
    {
        if (chunkDto.Chunk.Length == 0)
        {
            if (typeof(T) == typeof(string))
            {
                result = string.Empty as T;
            }
            else
            {
                result = default(T);
            }

            return true;
        }

        var chunks = AddToCache(chunkDto);

        if (!chunkDto.IsLastChunk)
        {
            result = default;

            return false;
        }

        var allBytes = CombineChunks(chunks);

        try
        {
            result = options != null ?
                MessagePackSerializer.Deserialize<T>(allBytes, options) :
                MessagePackSerializer.Deserialize<T>(allBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize data into the expected type", ex);
        }

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

    private static ConcurrentBag<ChunkDto> AddToCache(ChunkDto chunkDto)
    {
        var chunks = _cache.GetOrCreate(chunkDto.SequenceId, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(1);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            
            return new ConcurrentBag<ChunkDto>();
        });

        chunks.Add(chunkDto);

        if (chunkDto.IsLastChunk)
        {
            _cache.Remove(chunkDto.SequenceId);
        }

        return chunks;
    }

    private static byte[] CombineChunks(ConcurrentBag<ChunkDto> chunks)
    {
        var orderedChunks = chunks.OrderBy(c => c.ChunkId);
        var allBytes = new List<byte>();

        foreach (var chunk in orderedChunks)
        {
            allBytes.AddRange(chunk.Chunk);
        }

        return allBytes.ToArray();
    }
}
