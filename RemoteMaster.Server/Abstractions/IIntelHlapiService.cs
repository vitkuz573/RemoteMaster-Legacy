// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Intel.Manageability;
using Intel.Manageability.RemoteAccess;

namespace RemoteMaster.Server.Abstractions;

public interface IIntelHlapiService : IDisposable
{
    void Connect(string host, string username, string password, bool secure = false,
                        string? certificate = null, ConnectionInfoEX.AuthMethod auth = ConnectionInfoEX.AuthMethod.Digest,
                        ConnectionInfoEX.SocksProxy? proxy = null, ConnectionInfoEX.SocksProxy? redirectionProxy = null,
                        ConnectionInfoEX.TcpForwarder? tcpForwarder = null, bool acceptSelfSignedCertificate = false);

    void PowerDown();

    List<X509Certificate2> GetTrustedRootCertificates();

    void StartKvmSession(string kvmPassword);

    void StopKvmSession();

    void CreateAlertTrigger(string primaryMpsHost, ushort primaryMpsPort, string primaryMpsUsername, string primaryMpsPassword, string primaryMpsCert,
                        string secondaryMpsHost, ushort secondaryMpsPort, string secondaryMpsUsername, string secondaryMpsPassword, string secondaryMpsCert);

    void CreatePeriodicTrigger(uint intervalMinutes, uint dailyStartHour, uint dailyStartMinute, string mpsHost);

    void CreateUserInitiatedTrigger(uint timeoutSeconds, string mpsHost, ushort mpsPort, string certPath, string certPassword, string mpsPassword);
    
    ICollection<Trigger> GetAllTriggersDetails();
}
