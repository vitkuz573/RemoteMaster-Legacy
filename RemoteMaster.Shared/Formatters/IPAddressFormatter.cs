// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using MessagePack;
using MessagePack.Formatters;

namespace RemoteMaster.Shared.Formatters;

public class IPAddressFormatter : IMessagePackFormatter<IPAddress?>
{
    public void Serialize(ref MessagePackWriter writer, IPAddress? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
        }
        else
        {
            writer.Write(value.ToString());
        }
    }

    public IPAddress? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var ipString = reader.ReadString();

        if (IPAddress.TryParse(ipString, out var ipAddress))
        {
            return ipAddress;
        }

        throw new InvalidOperationException($"Invalid IP address format: '{ipString}'.");
    }
}
