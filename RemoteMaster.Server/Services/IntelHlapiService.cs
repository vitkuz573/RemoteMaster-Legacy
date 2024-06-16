// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security;
using System.Security.Cryptography.X509Certificates;
using Intel.Manageability;
using Intel.Manageability.KVM;
using Intel.Manageability.RemoteAccess;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class IntelHlapiService : IIntelHlapiService
{
    private IAMTInstance? _amtInstance;
    private bool _disposed = false;

    public void Connect(string host, string username, string password, bool secure = false, string? certificate = null, ConnectionInfoEX.AuthMethod auth = ConnectionInfoEX.AuthMethod.Digest, ConnectionInfoEX.SocksProxy? proxy = null, ConnectionInfoEX.SocksProxy? redirectionProxy = null, ConnectionInfoEX.TcpForwarder? tcpForwarder = null, bool acceptSelfSignedCertificate = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        using var securePassword = new SecureString();
        
        foreach (var c in password)
        {
            securePassword.AppendChar(c);
        }
        
        securePassword.MakeReadOnly();

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

    public void StartKvmSession(string kvmPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(kvmPassword);

        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        using var securePassword = new SecureString();

        foreach (var c in kvmPassword)
        {
            securePassword.AppendChar(c);
        }

        securePassword.MakeReadOnly();

        _amtInstance.KVMSetup.SetInterfaceState(true);
        _amtInstance.KVMSetup.SetPortsState(KVMPortsState.EnableDefaultPortOnly);
        _amtInstance.KVMSetup.SetRFBPassword(securePassword);
    }

    public void StopKvmSession()
    {
        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        _amtInstance.KVMSetup.SetInterfaceState(false);
    }

    public void CreateAlertTrigger(string primaryMpsHost, ushort primaryMpsPort, string primaryMpsUsername, string primaryMpsPassword, string primaryMpsCert, string secondaryMpsHost, ushort secondaryMpsPort, string secondaryMpsUsername, string secondaryMpsPassword, string secondaryMpsCert)
    {
        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        var primaryMps = new MPS(primaryMpsHost, primaryMpsPort, primaryMpsUsername, primaryMpsPassword, primaryMpsCert);
        var secondaryMps = new MPS(secondaryMpsHost, secondaryMpsPort, secondaryMpsUsername, secondaryMpsPassword, secondaryMpsCert);
        var alertTrigger = new AlertTrigger(0);
        
        _amtInstance.RemoteAccess.CreateTrigger(alertTrigger, primaryMps, secondaryMps);
    }

    public void CreatePeriodicTrigger(uint intervalMinutes, uint dailyStartHour, uint dailyStartMinute, string mpsHost)
    {
        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        var periodicTrigger = new PeriodicTrigger(intervalMinutes, new DailyInterval(dailyStartHour, dailyStartMinute));
        
        _amtInstance.RemoteAccess.CreateTrigger(periodicTrigger, mpsHost);
    }

    public void CreateUserInitiatedTrigger(uint timeoutSeconds, string mpsHost, ushort mpsPort, string certPath, string certPassword, string mpsPassword)
    {
        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        using var certificate = new X509Certificate2(certPath, certPassword, X509KeyStorageFlags.Exportable);

        var mps = new MPS(mpsHost, mpsPort, certificate, mpsPassword);
        var userInitiatedTrigger = new UserInitiatedTrigger(timeoutSeconds, UserInitiatedPermission.EnableAll);

        _amtInstance.RemoteAccess.CreateTrigger(userInitiatedTrigger, mps);
    }

    public ICollection<Trigger> GetAllTriggersDetails()
    {
        if (_amtInstance == null)
        {
            throw new InvalidOperationException("Not connected to any Intel AMT device.");
        }

        return _amtInstance.RemoteAccess.GetAllTriggersDetails();
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
