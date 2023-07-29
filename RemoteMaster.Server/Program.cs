using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Server.Core.Extensions;
using RemoteMaster.Server.Services;

var builder = WebApplication.CreateBuilder(args).ConfigureCoreUrls();

builder.Services.AddCoreServices();
builder.Services.AddSingleton<IScreenCapturer, BitBltCapturer>();
builder.Services.AddSingleton<IInputSender, InputSender>();

var app = builder.Build();

app.MapCoreHubs();

app.Run();