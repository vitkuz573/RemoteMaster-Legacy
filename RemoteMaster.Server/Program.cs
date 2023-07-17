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

builder.Services.AddScoped<IScreenCapturer, ScreenCapturer>();
builder.Services.AddScoped<IScreenCaster, ScreenCaster>();
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