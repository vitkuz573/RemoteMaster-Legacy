// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;

namespace RemoteMaster.Server.Abstractions;

public interface ICertificateService
{
    X509Certificate GenerateCertificateFromCSR(Pkcs10CertificationRequest csr);
}
