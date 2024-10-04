// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests.IntegrationTests;

public class ActiveDirectoryServiceTests
{
    [Fact]
    public void GetCaCertificate_ShouldReturnCertificate_WhenTemplateExists()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new ActiveDirectoryOptions
        {
            ActiveDirectoryServer = "localhost",
            TemplateName = "test-template",
            Username = "cn=admin,dc=test,dc=com",
            Password = "admin",
            SearchBase = "ou=Certificate Templates,dc=test,dc=com",
            KeySize = 2048,
            ValidityPeriod = 1
        });

        var service = new ActiveDirectoryCertificateAuthorityService(options);

        // Act
        var result = service.GetCaCertificate(X509ContentType.Pfx);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }
}