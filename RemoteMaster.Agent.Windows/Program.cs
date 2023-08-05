// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Extensions;
using RemoteMaster.Agent.Core.Models;
using RemoteMaster.Agent.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
}).ConfigureCoreUrls();

builder.Host.UseWindowsService();

builder.Services.AddCoreServices(builder.Configuration);
builder.Services.AddSingleton<ISignatureService, SignatureService>();
builder.Services.AddSingleton<IProcessService, ProcessService>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var serverSettings = app.Services.GetRequiredService<IOptions<ServerSettings>>().Value;

logger.LogInformation("Server settings: Path = {Path}, CertificateThumbprint = {Thumbprint}", serverSettings.Path, serverSettings.CertificateThumbprint);

app.MapCoreHubs();

app.Run();
