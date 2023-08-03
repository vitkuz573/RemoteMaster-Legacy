// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

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