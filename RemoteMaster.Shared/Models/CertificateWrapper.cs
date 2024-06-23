// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Models;

public class CertificateWrapper(X509Certificate2 certificate) : ICertificateWrapper
{
    public bool HasPrivateKey => certificate.HasPrivateKey;

    public string GetSerialNumberString() => certificate.GetSerialNumberString();

    public X509Certificate2 GetUnderlyingCertificate() => certificate;
}