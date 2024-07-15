// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http.Headers;

namespace RemoteMaster.Host.Core;

public class CustomHttpClientHandler : DelegatingHandler
{
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        if (!request.Headers.Accept.Any(h => h.MediaType == "application/vnd.remotemaster.v1+json"))
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.remotemaster.v1+json"));
        }

        if (request.Content != null && !request.Content.Headers.ContentType.MediaType.Equals("application/vnd.remotemaster.v1+json", StringComparison.OrdinalIgnoreCase))
        {
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.remotemaster.v1+json");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}