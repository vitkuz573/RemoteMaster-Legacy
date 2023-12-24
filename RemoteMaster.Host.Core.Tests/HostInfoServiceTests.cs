// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class HostInfoServiceTests
{
    private readonly HostInfoService _hostInfoService;

    public HostInfoServiceTests()
    {
        _hostInfoService = new HostInfoService();
    }

    [Fact]
    public void GetHostName_ShouldReturnNonEmptyString()
    {
        var result = _hostInfoService.GetHostName();
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    // Этот тест может завершиться неудачей в среде без сети
    [Fact]
    public void GetIPv4Address_ShouldReturnValidAddress()
    {
        var result = _hostInfoService.GetIPv4Address();
        Assert.False(string.IsNullOrWhiteSpace(result));
        // Дополнительно можно проверить, соответствует ли результат формату IPv4-адреса
    }

    // Этот тест может завершиться неудачей на машинах без сетевых интерфейсов
    [Fact]
    public void GetMacAddress_ShouldReturnValidMac()
    {
        var result = _hostInfoService.GetMacAddress();
        Assert.False(string.IsNullOrWhiteSpace(result));
        // Можно добавить проверку формата MAC-адреса
    }
}
