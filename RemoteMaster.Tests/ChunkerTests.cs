using RemoteMaster.Shared.Helpers;
using System.Text;

namespace RemoteMaster.Tests;

[TestClass]
public class ChunkerTests
{
    [TestMethod]
    public void Chunkify_StringData_ShouldReturnChunks()
    {
        var data = "This is some string data for testing.";

        var chunks = Chunker.Chunkify(data, 16).ToList();

        Assert.IsTrue(chunks.Count > 0);
    }

    [TestMethod]
    public void Chunkify_ByteData_ShouldReturnChunks()
    {
        var data = Encoding.ASCII.GetBytes("This is some byte data for testing.");

        var chunks = Chunker.Chunkify(data, 16).ToList();

        Assert.IsTrue(chunks.Count > 0);
    }

    [TestMethod]
    public void TryUnchunkify_StringData_ShouldReturnOriginalData()
    {
        var originalData = "This is some string data for testing.";
        var chunks = Chunker.Chunkify(originalData, 16).ToList();
        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out string result);

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
        var chunks = Chunker.Chunkify(originalData, 16).ToList();
        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out byte[] result);

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

        var chunks = Chunker.Chunkify(data, 16).ToList();
    }

    [TestMethod]
    public void TryUnchunkify_WrongType_ShouldThrowException()
    {
        var originalData = "This is some string data for testing.";
        var chunks = Chunker.Chunkify(originalData, 16).ToList();

        bool hasThrownException = false;
        try
        {
            foreach (var chunk in chunks)
            {
                Chunker.TryUnchunkify<Stream>(chunk, out _);
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

        var chunks = Chunker.Chunkify(data, 16).ToList();

        Assert.AreEqual(1, chunks.Count);
        Assert.AreEqual(0, chunks[0].Chunk.Length);
    }

    [TestMethod]
    public void TryUnchunkify_EmptyChunks_ShouldReturnEmptyString()
    {
        var originalData = "";
        var chunks = Chunker.Chunkify(originalData, 16).ToList();
        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out string result);

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

        var chunks = Chunker.Chunkify(data, 16).ToList();

        Assert.IsTrue(chunks.Count > 0);
    }

    [TestMethod]
    public void TryUnchunkify_VeryLargeChunks_ShouldReturnOriginalData()
    {
        var originalData = new string('x', 10000);
        var chunks = Chunker.Chunkify(originalData, 16).ToList();
        chunks.ForEach(chunk =>
        {
            Chunker.TryUnchunkify(chunk, out string result);

            if (chunk.IsLastChunk)
            {
                Assert.AreEqual(originalData, result);
            }
        });
    }
}
