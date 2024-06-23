// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;

namespace RemoteMaster.Shared.Abstractions;

public interface ICertificateStoreService
{
    IEnumerable<ICertificateWrapper> GetCertificates(StoreName storeName, StoreLocation storeLocation, X509FindType findType, string findValue);
}