// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Agent.Core.Abstractions;

namespace RemoteMaster.Agent.Services;

public class SignatureService : ISignatureService
{
    private readonly ILogger<SignatureService> _logger;

    public SignatureService(ILogger<SignatureService> logger)
    {
        _logger = logger;
    }

    public bool IsSignatureValid(string filePath, string expectedThumbprint)
    {
        using var cert = X509Certificate.CreateFromSignedFile(filePath);
        using var cert2 = new X509Certificate2(cert);
        using var chain = new X509Chain();

        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        var isChainValid = chain.Build(cert2);

        if (isChainValid)
        {
            if (cert2.Thumbprint.Equals(expectedThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Digital signature is valid.");
                return true;
            }
            else
            {
                _logger.LogWarning("Digital signature is valid but not from the expected certificate.");
                return false;
            }
        }
        else
        {
            _logger.LogWarning("Digital signature is not valid.");
            return false;
        }
    }
}
