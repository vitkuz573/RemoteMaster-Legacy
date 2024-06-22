// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateStoreService : ICertificateStoreService
{
    public X509Certificate2Collection GetCertificates(StoreName storeName, StoreLocation storeLocation, X509FindType findType, string findValue)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        return store.Certificates.Find(findType, findValue, false);
    }
}