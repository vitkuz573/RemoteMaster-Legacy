// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using FluentResults;

namespace RemoteMaster.Server.Abstractions;

public interface ICertificateAuthorityService
{
    Result EnsureCaCertificateExists();
    
    Result<X509Certificate2> GetCaCertificate(X509ContentType contentType);
}
