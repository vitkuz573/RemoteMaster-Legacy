// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http.Headers;

namespace RemoteMaster.Host.Core.HttpClientHandlers;

/// <summary>
/// A custom HTTP client handler that ensures the required Accept and Content-Type headers 
/// are set for the application/vnd.remotemaster.v1+json media type.
/// </summary>
public class CustomHttpClientHandler : DelegatingHandler
{
    private const string MediaType = "application/vnd.remotemaster.v1+json";

    /// <summary>
    /// Sends an HTTP request ensuring that the Accept and Content-Type headers are properly set.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The HTTP response message.</returns>
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (!request.Headers.Accept.Any(h => string.Equals(h.MediaType, MediaType, StringComparison.OrdinalIgnoreCase)))
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
        }

        if (request.Content != null)
        {
            var contentType = request.Content.Headers.ContentType;
            
            if (contentType == null || !string.Equals(contentType.MediaType, MediaType, StringComparison.OrdinalIgnoreCase))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
