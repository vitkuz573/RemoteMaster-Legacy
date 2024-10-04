// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class CertificateInfo(string issuer, string subject, string expirationDate, string effectiveDate, string signatureAlgorithm, string keySize, List<string> certificateChain)
{
    public string Issuer { get; } = issuer;

    public string Subject { get; } = subject;

    public string ExpirationDate { get; } = expirationDate;

    public string EffectiveDate { get; } = effectiveDate;

    public string SignatureAlgorithm { get; } = signatureAlgorithm;

    public string KeySize { get; } = keySize;

    public IReadOnlyList<string> CertificateChain { get; } = certificateChain.AsReadOnly();
}
