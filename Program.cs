using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Infrastructure.Repositories;
using GLMS.Enterprise.Services;
using GLMS.Enterprise.Services.Currency;
using GLMS.Enterprise.Services.Observers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;

// Running GLMS.Enterprise.exe from bin\Debug skips launchSettings.json — default to Development.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    && AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Caching ───────────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── HTTP Client ───────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("ExchangeRateApi", client =>
{
    client.BaseAddress = new Uri("https://api.exchangerate-api.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── Repository Pattern ────────────────────────────────────────────────────────
builder.Services.AddScoped<IContractRepository, ContractRepository>();

// ── Strategy Pattern: Currency ────────────────────────────────────────────────
// Strategy pattern — Transient per T8 (new instance per resolution)
builder.Services.AddTransient<LiveApiCurrencyStrategy>();
builder.Services.AddTransient<FixedRateStrategy>();
builder.Services.AddTransient<CachedCurrencyStrategy>(sp => new CachedCurrencyStrategy(
    sp.GetRequiredService<LiveApiCurrencyStrategy>(),
    sp.GetRequiredService<IMemoryCache>(),
    sp.GetRequiredService<ILogger<CachedCurrencyStrategy>>()));
builder.Services.AddTransient<ICurrencyStrategy>(sp => sp.GetRequiredService<CachedCurrencyStrategy>());

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IContractService>(sp =>
{
    var repo = sp.GetRequiredService<IContractRepository>();
    var notifier = sp.GetRequiredService<IContractStatusSubject>();
    return new ContractService(repo, notifier);
});
builder.Services.AddScoped<IExchangeRateApiService, ExchangeRateApiService>();
builder.Services.AddScoped<IFileService, FileService>();

// ── Observer Pattern: Contract Status Changes ───────────────────────────────
builder.Services.AddSingleton<IContractStatusObserver, AuditLogObserver>();
builder.Services.AddSingleton<IContractStatusObserver, EmailNotificationObserver>();
builder.Services.AddSingleton<IContractStatusSubject, ContractStatusNotifier>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var listensOnHttps = configuredUrls?.Contains("https://", StringComparison.OrdinalIgnoreCase) == true;
if (listensOnHttps)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ── Auto-migrate on startup (development convenience) ─────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            ?? app.Urls.FirstOrDefault()
            ?? "http://localhost:5000";

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Browser launch is best-effort in development.
        }
    });
}

app.Run();
