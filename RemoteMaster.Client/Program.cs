using Blazorise;
using Blazorise.FluentValidation;
using Blazorise.Icons.FontAwesome;
using Blazorise.Tailwind;
using FluentValidation;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client;
using RemoteMaster.Client.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IHubConnectionFactory, HubConnectionFactory>();

// Services
builder.Services.AddScoped<ControlFunctionsService>();
builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<ActiveDirectoryService>();
builder.Services.AddTransient<IHubConnectionBuilder>(s => new HubConnectionBuilder());
builder.Services.AddValidatorsFromAssembly(typeof(App).Assembly);
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Blazorise
builder.Services.AddBlazorise(options =>
{
    options.Immediate = true;
}).AddTailwindProviders().AddFontAwesomeIcons().AddBlazoriseFluentValidation();

// Blazor Pages and Server-side Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5254");

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
