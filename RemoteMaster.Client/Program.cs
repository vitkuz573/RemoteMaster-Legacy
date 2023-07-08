using Blazorise;
using Blazorise.FluentValidation;
using Blazorise.Icons.FontAwesome;
using Blazorise.Tailwind;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client;
using RemoteMaster.Client.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddBlazorise(options =>
{
    options.Immediate = true;
}).AddTailwindProviders().AddFontAwesomeIcons().AddBlazoriseFluentValidation();

builder.Services.AddValidatorsFromAssembly(typeof(App).Assembly);

builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<ActiveDirectoryService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
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
