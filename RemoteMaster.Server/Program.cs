using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared;
using RemoteMaster.Shared.Native.Windows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(builder =>
{
    builder.AddConsole().AddDebug();
    builder.AddEventLog();
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddProvider(new FileLoggerProvider("RemoteMaster_Server"));
});

builder.Services.AddRazorPages();
builder.Services.AddSignalR().AddMessagePackProtocol();

builder.Services.AddScoped<IScreenCaptureService, ScreenCaptureService>();
builder.Services.AddScoped<IScreenCasterService, ScreenCastService>();
builder.Services.AddScoped<IViewerService, ViewerService>();
builder.Services.AddScoped<IInputSender, InputSender>();
builder.Services.AddScoped<IAppStartup, AppStartup>();

var app = builder.Build();

var appStartup = app.Services.GetRequiredService<IAppStartup>();
await appStartup.Initialize();

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