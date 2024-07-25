// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class ApiService(IHttpClientFactory httpClientFactory, IHostConfigurationService hostConfigurationService, IHostInformationService hostInformationService) : IApiService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(nameof(ApiService));

    private const string CurrentApiVersion = "1.0";

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

            _client.BaseAddress = new Uri($"http://{hostConfiguration.Server}:5254");
        }
    }

    private async Task NotifyDeprecatedVersionAsync()
    {
        var hostInformation = hostInformationService.GetHostInformation();

        var message = new NotificationMessage(
            Id: Guid.NewGuid().ToString(),
            Title: "Deprecated API Version",
            Text: $"The current API version {CurrentApiVersion} is deprecated and will soon be unsupported. Please update to the latest version.",
            Category: "Warning",
            PublishDate: DateTime.UtcNow,
            Author: hostInformation.IpAddress
        );

        await AddNotificationAsync(message);
    }

    private async Task CheckDeprecatedVersionAsync(HttpResponseMessage response)
    {
        if (response.Headers.Contains("api-deprecated-versions"))
        {
            var deprecatedVersions = response.Headers.GetValues("api-deprecated-versions");

            if (deprecatedVersions.Contains(CurrentApiVersion))
            {
                await NotifyDeprecatedVersionAsync();
            }
        }
    }

    private async Task<T?> ProcessResponse<T>(HttpResponseMessage response) where T : class
    {
        await CheckDeprecatedVersionAsync(response);

#if DEBUG
        var responseBody = await response.Content.ReadAsStringAsync();

        Log.Information("Response Body: {ResponseBody}", responseBody);
#endif

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
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
            return apiResponse?.IsSuccess == true ? apiResponse.Data : null;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to read response as JSON: {Message}", ex.Message);
            return null;
        }
    }

    private async Task<bool> ProcessSimpleResponse(HttpResponseMessage response)
    {
        await CheckDeprecatedVersionAsync(response);

#if DEBUG
        var responseBody = await response.Content.ReadAsStringAsync();

        Log.Information("Response Body: {ResponseBody}", responseBody);
#endif

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        Log.Error("Request failed with status code {StatusCode}", response.StatusCode);

        var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();

        if (errorResponse?.Error != null)
        {
            Log.Error("Error details: {Error}", errorResponse.Error.ToString());
        }

        return false;

    }

    public async Task<bool> RegisterHostAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var response = await _client.PostAsJsonAsync("/api/Host/register", hostConfiguration);

        return await ProcessSimpleResponse(response);
    }

    public async Task<bool> UnregisterHostAsync()
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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/Host/unregister");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        return await ProcessSimpleResponse(response);
    }

    public async Task<bool> UpdateHostInformationAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var request = new HostUpdateRequest
        {
            MacAddress = hostConfiguration.Host.MacAddress,
            Organization = hostConfiguration.Subject.Organization,
            OrganizationalUnit = [.. hostConfiguration.Subject.OrganizationalUnit],
            IpAddress = hostConfiguration.Host.IpAddress,
            Name = hostConfiguration.Host.Name
        };

        var response = await _client.PutAsJsonAsync("/api/Host/update", request);

        return await ProcessSimpleResponse(response);
    }

    public async Task<bool> IsHostRegisteredAsync()
    {
        await EnsureClientInitializedAsync();

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        var response = await _client.GetAsync($"/api/Host/status?macAddress={hostConfiguration.Host.MacAddress}");

        return await ProcessSimpleResponse(response);
    }

    public async Task<byte[]?> GetJwtPublicKeyAsync()
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync("/api/Jwt/publicKey");

        return await ProcessResponse<byte[]>(response);
    }

    public async Task<byte[]?> GetCaCertificateAsync()
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync("/api/Certificate/ca");

        return await ProcessResponse<byte[]>(response);
    }

    public async Task<byte[]?> IssueCertificateAsync(byte[] csrBytes)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.PostAsJsonAsync("/api/Certificate/issue", csrBytes);

        return await ProcessResponse<byte[]>(response);
    }

    public async Task<HostMoveRequest?> GetHostMoveRequestAsync(string macAddress)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.GetAsync($"/api/HostMove?macAddress={macAddress}");

        return await ProcessResponse<HostMoveRequest>(response);
    }

    public async Task<bool> AcknowledgeMoveRequestAsync(string macAddress)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.PostAsJsonAsync("/api/HostMove/acknowledge", macAddress);

        return await ProcessSimpleResponse(response);
    }

    private async Task AddNotificationAsync(NotificationMessage message)
    {
        await EnsureClientInitializedAsync();

        var response = await _client.PostAsJsonAsync("/api/Notification", message);

        await ProcessSimpleResponse(response);
    }
}
