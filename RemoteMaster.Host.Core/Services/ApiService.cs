// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class ApiService(IHttpClientFactory httpClientFactory, IHostConfigurationService hostConfigurationService) : IApiService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(nameof(ApiService));

    private async Task EnsureClientInitializedAsync()
    {
        if (_client.BaseAddress == null)
        {
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

            if (hostConfiguration == null || string.IsNullOrEmpty(hostConfiguration.Server))
            {
                Log.Error("Server configuration is required");

                throw new ArgumentNullException(nameof(hostConfiguration.Server), "Server configuration is required");
            }

            _client.BaseAddress = new Uri($"http://{hostConfiguration.Server}");
        }
    }

    private static async Task<ApiResponse<T>?> ProcessResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Request failed with status code {StatusCode}", response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();

            if (errorResponse?.Error != null)
            {
                Log.Error("Error details: {Error}", errorResponse.Error.ToString());
            }

            return null;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to read response as JSON: {Message}", ex.Message);

            return null;
        }
    }

    public async Task<ApiResponse<bool>?> RegisterHostAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var response = await _client.PostAsJsonAsync("/api/Host/register", hostConfiguration);

        return await ProcessResponse<bool>(response);
    }

    public async Task<ApiResponse<bool>?> UnregisterHostAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var request = new HostUnregisterRequest
        {
            MacAddress = hostConfiguration.Host.MacAddress,
            Organization = hostConfiguration.Subject.Organization,
            OrganizationalUnit = [..hostConfiguration.Subject.OrganizationalUnit],
            Name = hostConfiguration.Host.Name
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/Host/unregister");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        return await ProcessResponse<bool>(response);
    }

    public async Task<ApiResponse<bool>?> UpdateHostInformationAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var request = new HostUpdateRequest
        {
            MacAddress = hostConfiguration.Host.MacAddress,
            Organization = hostConfiguration.Subject.Organization,
            OrganizationalUnit = [..hostConfiguration.Subject.OrganizationalUnit],
            IpAddress = hostConfiguration.Host.IpAddress,
            Name = hostConfiguration.Host.Name
        };

        var response = await _client.PutAsJsonAsync("/api/Host/update", request);

        return await ProcessResponse<bool>(response);
    }

    public async Task<ApiResponse<bool>?> IsHostRegisteredAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var response = await _client.GetAsync($"/api/Host/status?macAddress={hostConfiguration.Host.MacAddress}");

        return await ProcessResponse<bool>(response);
    }

    public async Task<ApiResponse<byte[]>?> GetJwtPublicKeyAsync()
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync("/api/Jwt");

        return await ProcessResponse<byte[]>(response);
    }

    public async Task<ApiResponse<byte[]>?> GetCaCertificateAsync()
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync("/api/Certificate/ca");

        return await ProcessResponse<byte[]>(response);
    }

    public async Task<ApiResponse<byte[]>?> IssueCertificateAsync(byte[] csrBytes)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.PostAsJsonAsync("/api/Certificate/issue", csrBytes);

        return await ProcessResponse<byte[]>(response);
    }

    public async Task<ApiResponse<HostMoveRequest>?> GetHostMoveRequestAsync(string macAddress)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync($"/api/HostMove?macAddress={macAddress}");

        return await ProcessResponse<HostMoveRequest>(response);
    }

    public async Task<ApiResponse<bool>?> AcknowledgeMoveRequestAsync(string macAddress)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.PostAsJsonAsync("/api/HostMove/acknowledge", macAddress);

        return await ProcessResponse<bool>(response);
    }
}
