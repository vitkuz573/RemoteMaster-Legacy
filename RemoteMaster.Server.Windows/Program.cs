// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Server.Core.Extensions;
using RemoteMaster.Server.Services;

var builder = WebApplication.CreateBuilder(args).ConfigureCoreUrls();

builder.Services.AddCoreServices();
builder.Services.AddSingleton<IScreenCapturer, BitBltCapturer>();
builder.Services.AddSingleton<IInputSender, InputSender>();
builder.Services.AddSingleton<IPowerManager, PowerManager>();

var app = builder.Build();

app.MapCoreHubs();

app.Run();