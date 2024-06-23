// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Shared.Services;

public class CertificateStoreService : ICertificateStoreService
{
    public IEnumerable<ICertificateWrapper> GetCertificates(StoreName storeName, StoreLocation storeLocation, X509FindType findType, string findValue)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(findType, findValue, false);

        return certificates.Cast<X509Certificate2>().Select(cert => new CertificateWrapper(cert)).ToList();
    }
}