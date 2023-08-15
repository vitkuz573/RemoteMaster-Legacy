// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class ConnectionManager : IConnectionManager
{
    private readonly Dictionary<string, object> _contexts = new();
    private readonly IServiceProvider _serviceProvider;

    public ConnectionManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IConnectionContext Connect(string name, string url, bool useMessagePack = false)
    {
        var context = _serviceProvider.GetRequiredService<IConnectionContext>().Configure(url, useMessagePack);
        _contexts[name] = context;

        return context;
    }

    public IConnectionContext Get(string name)
    {
        return _contexts.ContainsKey(name) ? _contexts[name] as IConnectionContext : null;
    }

    public async Task DisconnectAsync(string name)
    {
        if (_contexts.ContainsKey(name) && _contexts[name] is IConnectionContext context)
        {
            await context.StopAsync();
            _contexts.Remove(name);
        }
    }
}