// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.JsonContexts;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class ApiService(IHttpClientFactory httpClientFactory, IHostConfigurationProvider hostConfigurationProvider, IHostInformationService hostInformationService, ILogger<ApiService> logger) : IApiService
{
    private const string CurrentApiVersion = "1.0";
    private const string ApiDeprecatedVersionsHeader = "api-deprecated-versions";

    private HttpClient CreateClient(string server)
    {
        var client = httpClientFactory.CreateClient(nameof(ApiService));
        client.BaseAddress = new Uri($"http://{server}:5254");

        return client;
    }

    private async Task NotifyDeprecatedVersionAsync(HostConfiguration hostConfiguration)
    {
        try
        {
            var hostInformation = hostInformationService.GetHostInformation();

            var message = new NotificationMessage(
                Id: Guid.NewGuid().ToString(),
                Title: "Deprecated API Version",
                Text: $"The current API version {CurrentApiVersion} is deprecated and will soon be unsupported. Please update to the latest version.",
                Category: "Warning",
                PublishDate: DateTime.UtcNow,
                Author: hostInformation.IpAddress.ToString()
            );

            await AddNotificationAsync(hostConfiguration, message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to retrieve host information. Skipping deprecated version notification.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while attempting to send deprecated version notification.");
        }
    }

    private async Task CheckDeprecatedVersionAsync(HostConfiguration hostConfiguration, HttpResponseMessage response)
    {
        if (response.Headers.Contains(ApiDeprecatedVersionsHeader))
        {
            var deprecatedVersions = response.Headers.GetValues(ApiDeprecatedVersionsHeader);

            if (deprecatedVersions.Contains(CurrentApiVersion))
            {
                await NotifyDeprecatedVersionAsync(hostConfiguration);
            }
        }
    }

    private static JsonTypeInfo GetTypeInfoForApiResponseOfT<T>()
    {
        if (typeof(T) == typeof(byte[]))
        {
            return ApiJsonSerializerContext.Default.ApiResponseByteArray;
        }

        if (typeof(T) == typeof(OrganizationDto))
        {
            return ApiJsonSerializerContext.Default.ApiResponseOrganizationDto;
        }

        if (typeof(T) == typeof(HostMoveRequestDto))
        {
            return ApiJsonSerializerContext.Default.ApiResponseHostMoveRequestDto;
        }

        throw new NotSupportedException($"Type {typeof(T)} is not supported for deserialization.");
    }

    private async Task<T?> ProcessResponseAsync<T>(HostConfiguration hostConfiguration, HttpResponseMessage response) where T : class
    {
        await CheckDeprecatedVersionAsync(hostConfiguration, response);

        var responseBody = await response.Content.ReadAsStringAsync();

#if DEBUG
    logger.LogInformation("Response Body: {ResponseBody}", responseBody);
#endif

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Request failed with status code {StatusCode}", response.StatusCode);

            var errorResponse = JsonSerializer.Deserialize(responseBody, ApiJsonSerializerContext.Default.ApiResponse);

            if (errorResponse?.Error != null)
            {
                logger.LogError("Error details: {Error}", errorResponse.Error.ToString());
            }

            return null;
        }

        try
        {
            var jsonTypeInfo = GetTypeInfoForApiResponseOfT<T>();

            var apiResponse = (ApiResponse<T>)JsonSerializer.Deserialize(responseBody, jsonTypeInfo)!;

            return apiResponse.IsSuccess ? apiResponse.Data : null;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to read response as JSON: {Message}", ex.Message);

            return null;
        }
    }

    private async Task<bool> ProcessSimpleResponseAsync(HostConfiguration hostConfiguration, HttpResponseMessage response)
    {
        await CheckDeprecatedVersionAsync(hostConfiguration, response);

#if DEBUG
        var responseBody = await response.Content.ReadAsStringAsync();

        logger.LogInformation("Response Body: {ResponseBody}", responseBody);
#endif

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        logger.LogError("Request failed with status code {StatusCode}", response.StatusCode);

        var errorResponse = await response.Content.ReadFromJsonAsync(ApiJsonSerializerContext.Default.ApiResponse);

        if (errorResponse?.Error != null)
        {
            logger.LogError("Error details: {Error}", errorResponse.Error.ToString());
        }

        return false;
    }

    public async Task<bool> RegisterHostAsync(bool force)
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var request = new HostRegisterRequest(hostConfiguration, force);

        var response = await client.PostAsJsonAsync("/api/host", request, HostJsonSerializerContext.Default.HostRegisterRequest);

        return await ProcessSimpleResponseAsync(hostConfiguration, response);
    }

    public async Task<bool> UnregisterHostAsync()
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var request = new HostUnregisterRequest(hostConfiguration.Host.MacAddress, hostConfiguration.Subject.Organization, hostConfiguration.Subject.OrganizationalUnit);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/host");
        httpRequest.Content = JsonContent.Create(request, HostJsonSerializerContext.Default.HostUnregisterRequest);

        var response = await client.SendAsync(httpRequest);

        return await ProcessSimpleResponseAsync(hostConfiguration, response);
    }

    public async Task<bool> UpdateHostInformationAsync()
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var request = new HostUpdateRequest(hostConfiguration.Host.MacAddress, hostConfiguration.Subject.Organization, hostConfiguration.Subject.OrganizationalUnit, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);

        var response = await client.PutAsJsonAsync("/api/host", request, HostJsonSerializerContext.Default.HostUpdateRequest);

        return await ProcessSimpleResponseAsync(hostConfiguration, response);
    }

    public async Task<bool> IsHostRegisteredAsync()
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var response = await client.GetAsync($"/api/host/{hostConfiguration.Host.MacAddress}/status");

        return await ProcessSimpleResponseAsync(hostConfiguration, response);
    }

    public async Task<byte[]?> GetJwtPublicKeyAsync()
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var response = await client.GetAsync("/api/jwt");

        return await ProcessResponseAsync<byte[]>(hostConfiguration, response);
    }

    public async Task<byte[]?> GetCaCertificateAsync()
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var response = await client.GetAsync("/api/ca");

        return await ProcessResponseAsync<byte[]>(hostConfiguration, response);
    }

    public async Task<byte[]?> IssueCertificateAsync(byte[] csrBytes)
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var response = await client.PostAsJsonAsync("/api/certificate", csrBytes, ApiJsonSerializerContext.Default.ByteArray);

        return await ProcessResponseAsync<byte[]>(hostConfiguration, response);
    }

    public async Task<HostMoveRequestDto?> GetHostMoveRequestAsync(PhysicalAddress macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var response = await client.GetAsync($"/api/host/{macAddress}/moveRequest");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ProcessResponseAsync<HostMoveRequestDto>(hostConfiguration, response);
    }

    public async Task<bool> AcknowledgeMoveRequestAsync(PhysicalAddress macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/host/{macAddress}/moveRequest");
        var response = await client.SendAsync(request);

        return await ProcessSimpleResponseAsync(hostConfiguration, response);
    }

    private async Task AddNotificationAsync(HostConfiguration hostConfiguration, NotificationMessage message)
    {
        var client = CreateClient(hostConfiguration.Server);

        var response = await client.PostAsJsonAsync("/api/notification", message, NotificationJsonSerializerContext.Default.NotificationMessage);

        await ProcessSimpleResponseAsync(hostConfiguration, response);
    }

    public async Task<OrganizationDto?> GetOrganizationAsync(string name)
    {
        var hostConfiguration = hostConfigurationProvider.Current ?? throw new InvalidOperationException("HostConfiguration is not set.");

        var client = CreateClient(hostConfiguration.Server);

        var response = await client.GetAsync($"/api/organization/{name}");

        return await ProcessResponseAsync<OrganizationDto>(hostConfiguration, response);
    }
}
