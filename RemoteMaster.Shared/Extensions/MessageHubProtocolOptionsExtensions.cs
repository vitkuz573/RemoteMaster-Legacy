// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Shared.Extensions;

public static class MessagePackHubProtocolOptionsExtensions
{
    public static void Configure(this MessagePackHubProtocolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var formatters = new IMessagePackFormatter[]
        {
            new IPAddressFormatter(),
            new PhysicalAddressFormatter()
        };

        var resolver = CompositeResolver.Create(formatters, [ContractlessStandardResolver.Instance]);
        
        options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
    }
}
