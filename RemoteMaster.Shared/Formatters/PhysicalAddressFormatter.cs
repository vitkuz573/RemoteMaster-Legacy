// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using MessagePack;
using MessagePack.Formatters;

namespace RemoteMaster.Shared.Formatters;

public class PhysicalAddressFormatter : IMessagePackFormatter<PhysicalAddress>
{
    public void Serialize(ref MessagePackWriter writer, PhysicalAddress value, MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);

        writer.Write(value.ToString());
    }

    public PhysicalAddress Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var macString = reader.ReadString();

        return macString is null
            ? throw new InvalidOperationException("PhysicalAddress cannot be null during deserialization.")
            : PhysicalAddress.Parse(macString);
    }
}
