// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class ApiService(IHttpClientFactory httpClientFactory) : IApiService
{
    private HttpClient CreateClient(string server)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"http://{server}");
        
        return client;
    }

    public async Task<ApiResponse<Guid?>> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        using var client = CreateClient(hostConfiguration.Server);
        var response = await client.PostAsJsonAsync("/api/hostregistration/register", hostConfiguration);
        
        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid?>>();
    }

    public async Task<ApiResponse<bool>> UnregisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        using var client = CreateClient(hostConfiguration.Server);
        var response = await client.PostAsJsonAsync("/api/hostregistration/unregister", hostConfiguration);
        
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
    }

    public async Task<ApiResponse<bool>> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        using var client = CreateClient(hostConfiguration.Server);
        var response = await client.PostAsJsonAsync("/api/hostregistration/update", hostConfiguration);
        
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
    }

    public async Task<ApiResponse<bool>> IsHostRegisteredAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        using var client = CreateClient(hostConfiguration.Server);
        var response = await client.GetAsync($"/api/hostregistration/check?macAddress={hostConfiguration.Host.MacAddress}");
        
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
    }
}
