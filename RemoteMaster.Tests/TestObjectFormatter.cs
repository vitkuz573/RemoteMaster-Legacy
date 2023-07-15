using MessagePack;
using MessagePack.Formatters;

namespace RemoteMaster.Tests;

public class TestObjectFormatter : IMessagePackFormatter<TestObject>
{
    public void Serialize(ref MessagePackWriter writer, TestObject value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.Property1);
        writer.Write(value.Property2);
    }

    public TestObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var length = reader.ReadArrayHeader();

        if (length != 2)
        {
            throw new InvalidOperationException("Invalid format.");
        }

        var property1 = reader.ReadString();
        var property2 = reader.ReadInt32();

        return new TestObject { Property1 = property1, Property2 = property2 };
    }
}