// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http;
using System.Text;
using System.Text.Json;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using Serilog; // Убедитесь, что Serilog подключен

namespace RemoteMaster.Server.Services;

public class AuthService : IAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _apiClient;

    public AuthService(IHttpContextAccessor httpContextAccessor, HttpClient apiClient)
    {
        _httpContextAccessor = httpContextAccessor;
        _apiClient = apiClient;
        _apiClient.BaseAddress = new Uri("http://127.0.0.1/api/");
    }

    public async Task<bool> RefreshTokensAsync(string refreshToken)
    {
        Log.Error("Starting token refresh process.");

        var requestObject = new
        {
            RefreshToken = refreshToken
        };

        var jsonRequest = JsonSerializer.Serialize(requestObject);
        using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var requestUri = new Uri(_apiClient.BaseAddress, "auth/refresh-token");

        var response = await _apiClient.PostAsync(requestUri, content);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Log.Error($"Server responded with: {jsonResponse}");

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TokenResponseData>>(jsonResponse);

            if (apiResponse == null)
            {
                Log.Error("Deserialized API response is null.");
                return false;
            }

            if (apiResponse.Success)
            {
                var accessToken = apiResponse.Data?.AccessToken;
                var newRefreshToken = apiResponse.Data?.RefreshToken;

                if (!string.IsNullOrEmpty(accessToken))
                {
                    var accessTokenCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false, // Consider setting to true if using HTTPS
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddHours(2)
                    };

                    var refreshTokenCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false, // Consider setting to true if using HTTPS
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    };

                    _httpContextAccessor.HttpContext?.Response.Cookies.Append(CookieNames.AccessToken, accessToken, accessTokenCookieOptions);
                    _httpContextAccessor.HttpContext?.Response.Cookies.Append(CookieNames.RefreshToken, newRefreshToken, refreshTokenCookieOptions);

                    Log.Error("Tokens refreshed and cookies updated successfully.");
                    return true;
                }
                else
                {
                    Log.Error("AccessToken is null or empty.");
                }
            }
            else
            {
                Log.Error($"API response indicates failure: {apiResponse.Message}");
            }
        }
        else
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            Log.Error($"Failed to refresh token. StatusCode: {response.StatusCode}, Response: {errorResponse}");
        }

        return false;
    }
}
