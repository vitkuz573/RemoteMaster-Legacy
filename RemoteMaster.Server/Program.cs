using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Native.Windows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR().AddMessagePackProtocol();

builder.Services.AddScoped<IScreenCaptureService, ScreenCaptureService>();
builder.Services.AddScoped<IScreenCasterService, ScreenCastService>();
builder.Services.AddScoped<IViewerService, ViewerService>();
builder.Services.AddScoped<IInputSender, InputSender>();

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5076");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<ControlHub>("/hubs/control");

app.Run();

if (DesktopHelper.GetCurrentDesktop(out var currentDesktopName))
{
    // _logger.LogInformation("Setting initial desktop to {currentDesktopName}.", currentDesktopName);
}
else
{
    // _logger.LogWarning("Failed to get initial desktop name.");
}

if (!DesktopHelper.SwitchToInputDesktop())
{
    // _logger.LogWarning("Failed to set initial desktop.");
}
