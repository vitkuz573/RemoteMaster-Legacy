using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared;

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

// Регистрация Singleton services
builder.Services.AddSingleton<IScreenCapturer, ScreenCapturer>();
builder.Services.AddSingleton<IInputSender, InputSender>();
builder.Services.AddSingleton<IViewerStore, ViewerStore>();
builder.Services.AddSingleton<IShutdownService, ShutdownService>();
builder.Services.AddSingleton<IIdleTimer, IdleTimer>();

// Регистрация Scoped services
builder.Services.AddScoped<IScreenCaster, ScreenCaster>();

// Регистрация Transient services
builder.Services.AddTransient<IViewerFactory, ViewerFactory>();

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

// var viewerMonitorService = app.Services.GetRequiredService<IIdleTimer>();
// viewerMonitorService.StartMonitoring();

app.Run();