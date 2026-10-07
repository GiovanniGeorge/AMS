using Microsoft.AspNetCore.Authentication.Cookies;
using AMS.Application.Common;
using AMS.Infrastructure;
using AMS.Web.Components;
using AMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Presentation services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Authentication: a login cookie (default lifetime 14 days, renewed while the user is active).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Everything database-related lives in AMS.Infrastructure.
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Order matters: authentication, then authorization, then antiforgery.
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
