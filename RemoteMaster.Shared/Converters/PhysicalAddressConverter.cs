// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RemoteMaster.Shared.Converters;

public class PhysicalAddressConverter : JsonConverter<PhysicalAddress>
{
    public override PhysicalAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var macString = reader.GetString();

        return PhysicalAddress.Parse(macString!);
    }

    public override void Write(Utf8JsonWriter writer, PhysicalAddress value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStringValue(value.ToString());
    }
}
