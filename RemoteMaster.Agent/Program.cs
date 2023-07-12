using Microsoft.Extensions.Hosting.WindowsServices;
using RemoteMaster.Agent.Hubs;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

builder.Host.UseWindowsService();

builder.Services.AddSignalR();

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:3564");

app.MapHub<MainHub>("/hubs/main");

app.Run();
