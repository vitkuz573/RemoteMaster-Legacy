// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RemoteMaster.Host.Core.Abstractions;

public interface ICertificateRequestService
{
    byte[] GenerateSigningRequest(X500DistinguishedName subjectName, List<IPAddress> ipAddresses, out RSA rsaKeyPair);
}
