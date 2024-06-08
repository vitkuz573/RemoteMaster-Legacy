// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security;
using System.Security.Cryptography.X509Certificates;
using Intel.Manageability;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class IntelHlapiService : IIntelHlapiService
{
    private IAMTInstance? _amtInstance;
    private bool _disposed = false;

    public void Connect(string host, string username, string password, bool secure = false,
                        string? certificate = null, ConnectionInfoEX.AuthMethod auth = ConnectionInfoEX.AuthMethod.Digest,
                        ConnectionInfoEX.SocksProxy? proxy = null, ConnectionInfoEX.SocksProxy? redirectionProxy = null,
                        ConnectionInfoEX.TcpForwarder? tcpForwarder = null, bool acceptSelfSignedCertificate = false)
    {
        ArgumentNullException.ThrowIfNull(password);

        using var securePassword = new SecureString();

        foreach (var c in password)
        {
            securePassword.AppendChar(c);
        }

        using var connection = new ConnectionInfoEX(host, username, securePassword, secure, certificate, auth, proxy, redirectionProxy, tcpForwarder, acceptSelfSignedCertificate);
        _amtInstance = AMTInstanceFactory.CreateEX(connection);
    }

    public void PowerDown()
    {
        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        _amtInstance.Power.PowerDown();
    }

    public List<X509Certificate2> GetTrustedRootCertificates()
    {
        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        return _amtInstance.Config.CertificateManagement.GetTrustedRootCertificates();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _amtInstance?.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
