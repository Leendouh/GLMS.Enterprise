using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Infrastructure.Repositories;
using GLMS.Enterprise.Services;
using GLMS.Enterprise.Services.Currency;
using GLMS.Enterprise.Services.Observers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("GLMS.Enterprise.Web")));

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
