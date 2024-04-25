// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.JSInterop;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class AccessTokenProvider(ITokenService tokenService, IJSRuntime jsRuntime) : IAccessTokenProvider
{
    public async Task<string> GetAccessTokenAsync()
    {        
        var accessToken = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
        
        if (!string.IsNullOrEmpty(accessToken) && tokenService.IsTokenValid(accessToken))
        {
            return accessToken;
        }

        var refreshToken = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");
       
        if (!string.IsNullOrEmpty(refreshToken) && tokenService.IsRefreshTokenValid(refreshToken))
        {
            var newAccessToken = await tokenService.RefreshAccessToken(refreshToken);
            
            if (!string.IsNullOrEmpty(newAccessToken))
            {
                await jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", newAccessToken);
                
                return newAccessToken;
            }
        }

        throw new InvalidOperationException("No valid access token available.");
    }
}
