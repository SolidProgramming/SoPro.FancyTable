using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using SoPro.FancyTable.BrowserHost.Components;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.None);
// A benchmark fixture, intentionally unavailable on non-loopback interfaces.
builder.WebHost.ConfigureKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapGet("/health", () => "ready");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
