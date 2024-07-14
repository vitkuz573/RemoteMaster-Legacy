// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class ApiService(IHttpClientFactory httpClientFactory, IHostConfigurationService hostConfigurationService) : IApiService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient();

    private async Task EnsureClientInitializedAsync()
    {
        if (_client.BaseAddress == null)
        {
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();
            
            if (hostConfiguration == null || string.IsNullOrEmpty(hostConfiguration.Server))
            {
                throw new ArgumentNullException(nameof(hostConfiguration.Server), "Server configuration is required");
            }
            
            _client.BaseAddress = new Uri($"http://{hostConfiguration.Server}");
        }
    }

    public async Task<ApiResponse<bool>> RegisterHostAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var response = await _client.PostAsJsonAsync("/api/hostregistration/register", hostConfiguration);
        var responseContent = await response.Content.ReadAsStringAsync();

        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
    }

    public async Task<ApiResponse<bool>> UnregisterHostAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var request = new HostUnregisterRequest
        {
            MacAddress = hostConfiguration.Host.MacAddress,
            Organization = hostConfiguration.Subject.Organization,
            OrganizationalUnit = [.. hostConfiguration.Subject.OrganizationalUnit],
            Name = hostConfiguration.Host.Name
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/hostregistration/unregister")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _client.SendAsync(httpRequest);

        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
    }

    public async Task<ApiResponse<bool>> UpdateHostInformationAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();

        var request = new HostUpdateRequest
        {
            MacAddress = hostConfiguration.Host.MacAddress,
            Organization = hostConfiguration.Subject.Organization,
            OrganizationalUnit = [.. hostConfiguration.Subject.OrganizationalUnit],
            IpAddress = hostConfiguration.Host.IpAddress,
            Name = hostConfiguration.Host.Name
        };

        var response = await _client.PutAsJsonAsync("/api/hostregistration/update", request);

        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
    }

    public async Task<ApiResponse<bool>> IsHostRegisteredAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();

        var response = await _client.GetAsync($"/api/hostregistration/check?macAddress={hostConfiguration.Host.MacAddress}");

        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
    }

    public async Task<ApiResponse<byte[]>> GetJwtPublicKeyAsync()
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync("/api/jwtkey");

        return await response.Content.ReadFromJsonAsync<ApiResponse<byte[]>>();
    }

    public async Task<ApiResponse<byte[]>> GetCaCertificateAsync()
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync("/api/cacertificate");

        return await response.Content.ReadFromJsonAsync<ApiResponse<byte[]>>();
    }

    public async Task<ApiResponse<byte[]>> IssueCertificateAsync(byte[] csrBytes)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.PostAsJsonAsync("/api/certificate/issue", csrBytes);

        return await response.Content.ReadFromJsonAsync<ApiResponse<byte[]>>();
    }
}
