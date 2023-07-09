using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR().AddMessagePackProtocol();

builder.Services.AddScoped<IScreenCaptureService, ScreenCaptureService>();
builder.Services.AddScoped<IStreamingService, StreamingService>();
builder.Services.AddScoped<IScreenService, ScreenService>();

var app = builder.Build();

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
app.MapControllers();

app.Run();
