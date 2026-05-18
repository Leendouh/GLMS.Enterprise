using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Infrastructure.Repositories;
using GLMS.Enterprise.Services;
using GLMS.Enterprise.Services.Currency;
using GLMS.Enterprise.Services.Observers;
using Microsoft.EntityFrameworkCore;

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
// Register all three concrete strategies
builder.Services.AddScoped<LiveApiCurrencyStrategy>();
builder.Services.AddScoped<FixedRateStrategy>();
// CachedCurrencyStrategy wraps the live strategy — primary implementation
builder.Services.AddScoped<CachedCurrencyStrategy>();
// Resolve ICurrencyStrategy → CachedCurrencyStrategy (controllers use the interface, not the concrete type)
builder.Services.AddScoped<ICurrencyStrategy>(sp => sp.GetRequiredService<CachedCurrencyStrategy>());

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
builder.Services.AddSingleton<IContractStatusSubject, ContractStatusNotifier>();
builder.Services.AddSingleton<IContractStatusObserver, AuditLogObserver>();
builder.Services.AddSingleton<IContractStatusObserver, EmailNotificationObserver>();

// Register observers with the subject
builder.Services.AddSingleton(sp =>
{
    var subject = sp.GetRequiredService<IContractStatusSubject>();
    var observers = sp.GetServices<IContractStatusObserver>();
    foreach (var observer in observers)
    {
        subject.RegisterObserver(observer);
    }
    return subject;
});

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

app.Run();
