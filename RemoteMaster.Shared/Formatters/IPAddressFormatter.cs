// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using MessagePack;
using MessagePack.Formatters;

namespace RemoteMaster.Shared.Formatters;

public class IPAddressFormatter : IMessagePackFormatter<IPAddress>
{
    public void Serialize(ref MessagePackWriter writer, IPAddress value, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);

        writer.Write(value.ToString());
    }

    public IPAddress Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var ipString = reader.ReadString();

        return ipString is null
            ? throw new InvalidOperationException("IP address cannot be null during deserialization.")
            : IPAddress.Parse(ipString);
    }
}
