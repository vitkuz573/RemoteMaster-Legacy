// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Extensions;
using RemoteMaster.Client.Helpers;
using RemoteMaster.Client.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Services;

var builder = WebApplication.CreateBuilder(args).ConfigureCoreUrls();

builder.Services.AddCoreServices();
builder.Services.AddSingleton<IScreenCapturerService, BitBltCapturer>();
builder.Services.AddSingleton<IScreenRecorderService, ScreenRecorderService>();
builder.Services.AddSingleton<ICursorRenderService, CursorRenderService>();
builder.Services.AddSingleton<IInputService, InputService>();
builder.Services.AddSingleton<IPowerService, PowerService>();
builder.Services.AddSingleton<IHardwareService, HardwareService>();
builder.Services.AddSingleton<IServiceManager, ServiceManager>();

var publicKeyPath = @"C:\RemoteMaster\Security\public_key.pem";
var publicKey = File.ReadAllText(publicKeyPath);
using var rsa = new RSACryptoServiceProvider();
rsa.ImportFromPem(publicKey.ToCharArray());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };
    });


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapCoreHubs();

app.Run();