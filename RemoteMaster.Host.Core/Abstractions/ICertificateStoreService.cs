// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;

namespace RemoteMaster.Host.Core.Abstractions;

public interface ICertificateStoreService
{
    X509Certificate2Collection GetCertificates(StoreName storeName, StoreLocation storeLocation, X509FindType findType, string findValue);
}