// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Extensions;
using RemoteMaster.Client.Services;

if (args.Length > 0 && args[0] == "install")
{
    Console.WriteLine("Do you want to install?");
    Console.ReadLine();
    Environment.Exit(0);
}

var builder = WebApplication.CreateBuilder(args).ConfigureCoreUrls();

builder.Services.AddCoreServices();
builder.Services.AddSingleton<IScreenCapturerService, BitBltCapturer>();
builder.Services.AddSingleton<ICursorRenderService, CursorRenderService>();
builder.Services.AddSingleton<IInputService, InputService>();
builder.Services.AddSingleton<IPowerService, PowerService>();

var app = builder.Build();

app.MapCoreHubs();

app.Run();
