using System.Text;
using MessagePack.Resolvers;
using RemoteMaster.Shared.Helpers;

namespace RemoteMaster.Tests;

[TestClass]
public class ChunkerTests
{
    [TestMethod]
    public void Chunkify_StringData_ShouldReturnChunks()
    {
        var data = "This is some string data for testing.";
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, 16, resolver).ToList();

        Assert.IsTrue(chunks.Count > 0);
    }

    [TestMethod]
    public void Chunkify_ByteData_ShouldReturnChunks()
    {
        var data = Encoding.ASCII.GetBytes("This is some byte data for testing.");
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, 16, resolver).ToList();

        Assert.IsTrue(chunks.Count > 0);
    }

    [TestMethod]
    public void TryUnchunkify_StringData_ShouldReturnOriginalData()
    {
        var originalData = "This is some string data for testing.";
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(originalData, 16, resolver).ToList();

        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out string result, resolver);

            if (chunk.IsLastChunk)
            {
                Assert.AreEqual(originalData, result);
            }
        });
    }

    [TestMethod]
    public void TryUnchunkify_ByteData_ShouldReturnOriginalData()
    {
        var originalData = Encoding.ASCII.GetBytes("This is some byte data for testing.");
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(originalData, 16, resolver).ToList();

        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out byte[] result, resolver);

            if (chunk.IsLastChunk)
            {
                Assert.IsTrue(originalData.SequenceEqual(result));
            }
        });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Chunkify_NullObject_ShouldThrowException()
    {
        string data = null;
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, 16, resolver).ToList();
    }

    [TestMethod]
    public void TryUnchunkify_WrongType_ShouldThrowException()
    {
        var originalData = "This is some string data for testing.";
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(originalData, 16, resolver).ToList();

        bool hasThrownException = false;

        try
        {
            foreach (var chunk in chunks)
            {
                Chunker.TryUnchunkify<Stream>(chunk, out _, resolver);
            }
        }
        catch (InvalidOperationException)
        {
            hasThrownException = true;
        }

        Assert.IsTrue(hasThrownException);
    }

    [TestMethod]
    public void Chunkify_EmptyString_ShouldReturnEmptyChunks()
    {
        var data = "";
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, 16, resolver).ToList();

        Assert.AreEqual(1, chunks.Count);
        Assert.AreEqual(0, chunks[0].Chunk.Length);
    }

    [TestMethod]
    public void TryUnchunkify_EmptyChunks_ShouldReturnEmptyString()
    {
        var originalData = "";
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(originalData, 16, resolver).ToList();

        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out string result, resolver);

            if (chunk.IsLastChunk)
            {
                Assert.AreEqual(originalData, result);
            }
        });
    }

    [TestMethod]
    public void Chunkify_VeryLargeData_ShouldReturnChunks()
    {
        var data = new string('x', 10000);
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, 16, resolver).ToList();

        Assert.IsTrue(chunks.Count > 0);
    }

    [TestMethod]
    public void TryUnchunkify_VeryLargeChunks_ShouldReturnOriginalData()
    {
        var originalData = new string('x', 10000);
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(originalData, 16, resolver).ToList();

        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out string result, resolver);

            if (chunk.IsLastChunk)
            {
                Assert.AreEqual(originalData, result);
            }
        });
    }

    [TestMethod]
    public void Chunkify_CustomObjectData_ShouldReturnChunks()
    {
        var data = new TestObject
        {
            Property1 = "Property1 value",
            Property2 = 1234
        };
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, 16, resolver).ToList();

        Assert.IsTrue(chunks.Count > 0);
    }

    [TestMethod]
    public void TryUnchunkify_CustomObjectData_ShouldReturnOriginalData()
    {
        var originalData = new TestObject
        {
            Property1 = "Property1 value",
            Property2 = 1234
        };
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(originalData, 16, resolver).ToList();

        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out TestObject result, resolver);

            if (chunk.IsLastChunk)
            {
                Assert.AreEqual(originalData.Property1, result.Property1);
                Assert.AreEqual(originalData.Property2, result.Property2);
            }
        });
    }

    [TestMethod]
    public void Chunkify_ChunkSizeGreaterThanData_ShouldReturnSingleChunk()
    {
        var data = "Test data";
        var chunkSize = data.Length + 10;
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, chunkSize, resolver).ToList();

        Assert.AreEqual(1, chunks.Count);
    }

    [TestMethod]
    public void Chunkify_ChunkSizeLessThanData_ShouldReturnMultipleChunks()
    {
        var data = "This is some test data";
        var chunkSize = 10;
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(data, chunkSize, resolver).ToList();

        Assert.IsTrue(chunks.Count > 1);
    }

    [TestMethod]
    public void TryUnchunkify_ChunksOutOfOrder_ShouldThrowException()
    {
        var originalData = "This is some string data for testing.";
        var resolver = ContractlessStandardResolver.Instance;

        var chunks = Chunker.Chunkify(originalData, 16, resolver).ToList();
        chunks = chunks.OrderBy(x => Guid.NewGuid()).ToList();

        bool hasThrownException = false;

        try
        {
            foreach (var chunk in chunks)
            {
                Chunker.TryUnchunkify(chunk, out string result, resolver);
            }
        }
        catch (InvalidOperationException)
        {
            hasThrownException = true;
        }

        Assert.IsTrue(hasThrownException);
    }
}