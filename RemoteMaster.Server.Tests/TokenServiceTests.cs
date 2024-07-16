// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class TokenServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IOptions<JwtOptions>> _mockOptions;
    private readonly Mock<IClaimsService> _mockClaimsService;
    private readonly ApplicationDbContext _context;
    private readonly JwtOptions _options;
    private readonly TokenService _tokenService;
    private readonly MockFileSystem _mockFileSystem;

    public TokenServiceTests()
    {
        _mockUserManager = MockUserManager<ApplicationUser>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockClaimsService = new Mock<IClaimsService>();

        _options = new JwtOptions
        {
            KeysDirectory = "TestKeys",
            KeySize = 2048,
            KeyPassword = "TestPassword"
        };

        _mockOptions = new Mock<IOptions<JwtOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_options);

        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c.GetConnectionString("DefaultConnection")).Returns("Data Source=:memory:");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(configurationMock.Object.GetConnectionString("DefaultConnection"))
            .Options;

        _context = new ApplicationDbContext(configurationMock.Object);

        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes("TestPassword"), new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100000));
        var publicKey = rsa.ExportRSAPublicKey();

        _mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "TestKeys/private_key.der", new MockFileData(privateKey) },
            { "TestKeys/public_key.der", new MockFileData(publicKey) }
        });

        _tokenService = new TokenService(_mockOptions.Object, _context, _mockHttpContextAccessor.Object, _mockUserManager.Object, _mockClaimsService.Object, _mockFileSystem);
    }

    [Fact]
    public async Task GenerateTokensAsync_GeneratesTokensSuccessfully()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser"
        };

        _mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "User" });

        _mockClaimsService.Setup(cs => cs.GetClaimsForUserAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "User")
        });

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        var tokenData = await _tokenService.GenerateTokensAsync(user.Id);

        // Assert
        Assert.NotNull(tokenData.AccessToken);
        Assert.NotNull(tokenData.RefreshToken);
    }

    [Fact]
    public async Task RevokeAllRefreshTokensAsync_RevokesTokensSuccessfully()
    {
        // Arrange
        var userId = "test-user-id";
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(1),
            Created = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        await _tokenService.RevokeAllRefreshTokensAsync(userId, TokenRevocationReason.UserLoggedOut);

        // Assert
        var revokedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);

        Assert.NotNull(revokedToken);
        Assert.NotNull(revokedToken.Revoked);
    }

    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();

        return new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
    }
}
