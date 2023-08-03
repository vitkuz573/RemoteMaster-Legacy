// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using MessagePack;
using Microsoft.Extensions.Caching.Memory;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Shared.Helpers;

public static class Chunker
{
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public static IEnumerable<ChunkWrapper> Chunkify<T>(T data, int chunkSize = 4096, IFormatterResolver? resolver = null) where T : class
    {
        if (data == null)
        {
            throw new InvalidOperationException("Data cannot be null");
        }

        byte[] serializedData;
        var options = MessagePackSerializerOptions.Standard;

        if (resolver != null)
        {
            options = options.WithResolver(resolver);
        }

        if (data is string str && string.IsNullOrEmpty(str))
        {
            yield return new ChunkWrapper
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
            serializedData = MessagePackSerializer.Serialize(data, options);
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

    public static IEnumerable<ChunkWrapper> ChunkifyBytes(byte[] data, int chunkSize = 4096)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return GenerateChunks(data, chunkSize);
    }

    private static IEnumerable<ChunkWrapper> GenerateChunks(byte[] data, int chunkSize)
    {
        var chunkCount = (int)Math.Ceiling((double)data.Length / chunkSize);
        var sequenceId = Guid.NewGuid().ToString();

        for (var i = 0; i < chunkCount; i++)
        {
            var length = i < chunkCount - 1 ? chunkSize : data.Length - i * chunkSize;
            var chunk = new byte[length];

            Array.Copy(data, i * chunkSize, chunk, 0, length);

            yield return new ChunkWrapper
            {
                Chunk = chunk,
                IsFirstChunk = i == 0,
                IsLastChunk = i == chunkCount - 1,
                ChunkId = i,
                SequenceId = sequenceId
            };
        }
    }

    public static bool TryUnchunkify<T>(ChunkWrapper chunkDto, out T result, IFormatterResolver? resolver = null) where T : class
    {
        if (chunkDto == null)
        {
            throw new ArgumentNullException(nameof(chunkDto));
        }

        if (chunkDto.Chunk == null)
        {
            throw new ArgumentNullException(nameof(chunkDto.Chunk));
        }

        if (chunkDto.Chunk.Length == 0)
        {
            if (typeof(T) == typeof(string))
            {
                result = string.Empty as T;
            }
            else
            {
                result = default;
            }

            return true;
        }

        var chunks = AddToCache(chunkDto);
        var options = MessagePackSerializerOptions.Standard;

        if (resolver != null)
        {
            options = options.WithResolver(resolver);
        }

        if (!chunkDto.IsLastChunk)
        {
            result = default;

            return false;
        }

        var allBytes = CombineChunks(chunks);

        try
        {
            result = MessagePackSerializer.Deserialize<T>(allBytes, options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize data into the expected type", ex);
        }

        return true;
    }

    public static bool TryUnchunkify(ChunkWrapper chunkDto, out byte[] result)
    {
        if (chunkDto == null)
        {
            throw new ArgumentNullException(nameof(chunkDto));
        }

        if (chunkDto.Chunk == null)
        {
            throw new ArgumentNullException(nameof(chunkDto.Chunk));
        }

        var chunks = AddToCache(chunkDto);

        if (!chunkDto.IsLastChunk)
        {
            result = default;

            return false;
        }

        result = CombineChunks(chunks);

        return true;
    }

    private static ConcurrentBag<ChunkWrapper> AddToCache(ChunkWrapper chunkDto)
    {
        var chunks = _cache.GetOrCreate(chunkDto.SequenceId, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(1);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            return new ConcurrentBag<ChunkWrapper>();
        }) ?? throw new InvalidOperationException("Chunks cannot be null");

        chunks.Add(chunkDto);

        if (chunkDto.IsLastChunk)
        {
            _cache.Remove(chunkDto.SequenceId);
        }

        return chunks;
    }

    private static byte[] CombineChunks(ConcurrentBag<ChunkWrapper> chunks)
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
