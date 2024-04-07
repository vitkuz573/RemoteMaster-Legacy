// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class AuthService(IHttpContextAccessor httpContextAccessor, HttpClient httpClient) : IAuthService
{
    public async Task<bool> RefreshTokensAsync(string refreshToken)
    {
        var response = await httpClient.PostAsJsonAsync("http://127.0.0.1:5254/api/auth/refresh-token", new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();

            if (apiResponse != null && apiResponse.Success)
            {
                var tokens = JsonSerializer.Deserialize<TokenResponseData>(apiResponse.Data.ToString());
                var accessToken = tokens?.AccessToken;

                var сookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(2)
                };

                var httpContext = httpContextAccessor.HttpContext;

                httpContext.Response.Cookies.Append("accessToken", accessToken, сookieOptions);

                return true;
            }
        }

        return false;
    }
}
