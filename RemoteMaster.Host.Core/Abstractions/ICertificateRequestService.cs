// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;

namespace RemoteMaster.Host.Core.Abstractions;

public interface ICertificateRequestService
{
    Pkcs10CertificationRequest GenerateCSR(string subjectName, out AsymmetricCipherKeyPair keyPair);
}
