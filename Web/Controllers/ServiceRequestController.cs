using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Enterprise.Web.Controllers;

public class ServiceRequestController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IContractService     _contractService;
    private readonly ICurrencyStrategy    _currencyStrategy;
    private readonly IExchangeRateApiService _rateService;

    public ServiceRequestController(
        ApplicationDbContext     db,
        IContractService         contractService,
        ICurrencyStrategy        currencyStrategy,
        IExchangeRateApiService  rateService)
    {
        _db               = db;
        _contractService  = contractService;
        _currencyStrategy = currencyStrategy;
        _rateService      = rateService;
    }

    // ── Index ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var requests = await _db.ServiceRequests
            .Include(r => r.Contract)
                .ThenInclude(c => c.Client)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(requests);
    }

    // ── Details ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var request = await _db.ServiceRequests
            .Include(r => r.Contract)
                .ThenInclude(c => c.Client)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return NotFound();
        return View(request);
    }

    // ── Create GET ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Create(Guid? contractId)
    {
        // Fetch live rate for display
        var rate = await _rateService.GetUsdToZarRateAsync();

        var vm = new ServiceRequestViewModel
        {
            ContractId          = contractId ?? Guid.Empty,
            CurrentExchangeRate = rate,
            RateFromApi         = true
        };
        await PopulateContractDropdown(vm);
        return View(vm);
    }

    // ── Create POST ───────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRequestViewModel model)
    {
        // Workflow rule: check contract status before saving
        if (model.ContractId != Guid.Empty)
        {
            var canCreate = await _contractService.CanCreateServiceRequestAsync(model.ContractId);
            if (!canCreate)
            {
                var contract = await _db.Contracts.FindAsync(model.ContractId);
                var status   = contract?.Status.ToString() ?? "Unknown";
                ModelState.AddModelError(string.Empty,
                    $"Cannot create a service request. The contract is currently '{status}'. " +
                    "Only Active contracts accept new service requests.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.CurrentExchangeRate = await _rateService.GetUsdToZarRateAsync();
            await PopulateContractDropdown(model);
            return View(model);
        }

        // Currency conversion via Strategy Pattern
        decimal exchangeRate;
        decimal amountZar;
        try
        {
            exchangeRate = await _currencyStrategy.GetRateAsync("USD", "ZAR");
            amountZar    = await _currencyStrategy.ConvertAsync(model.AmountUSD, "USD", "ZAR");
        }
        catch (Exception)
        {
            // Graceful degradation — use fallback
            exchangeRate = 18.50m;
            amountZar    = Math.Round(model.AmountUSD * exchangeRate, 2);
            TempData["Warning"] = "Unable to fetch live exchange rate. Used cached/fallback rate of 18.50.";
        }

        var request = new ServiceRequest
        {
            Id              = Guid.NewGuid(),
            ContractId      = model.ContractId,
            Description     = model.Description,
            AmountUSD       = model.AmountUSD,
            AmountZAR       = amountZar,
            ExchangeRateUsed = exchangeRate,
            Status          = ServiceRequestStatus.Pending,
            CreatedBy       = model.CreatedBy ?? "System",
            CreatedAt       = DateTime.UtcNow
        };

        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Service request created. USD {model.AmountUSD:F2} → ZAR {amountZar:F2} (rate: {exchangeRate:F4}).";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit GET ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Edit(Guid id)
    {
        var request = await _db.ServiceRequests
            .Include(r => r.Contract)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return NotFound();

        var vm = new ServiceRequestViewModel
        {
            Id               = request.Id,
            ContractId       = request.ContractId,
            Description      = request.Description,
            AmountUSD        = request.AmountUSD,
            AmountZAR        = request.AmountZAR,
            ExchangeRateUsed = request.ExchangeRateUsed,
            Status           = request.Status,
            CreatedBy        = request.CreatedBy,
            CreatedAt        = request.CreatedAt,
            CurrentExchangeRate = request.ExchangeRateUsed
        };
        await PopulateContractDropdown(vm);
        return View(vm);
    }

    // ── Edit POST ─────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServiceRequestViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateContractDropdown(model);
            return View(model);
        }

        var request = await _db.ServiceRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Description = model.Description;
        request.Status      = model.Status;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Service request updated.";
        return RedirectToAction(nameof(Index));
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = await _db.ServiceRequests
            .Include(r => r.Contract).ThenInclude(c => c.Client)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var request = await _db.ServiceRequests.FindAsync(id);
        if (request == null) return NotFound();
        _db.ServiceRequests.Remove(request);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Service request deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ── API: live exchange rate for JS ────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetExchangeRate()
    {
        try
        {
            var rate = await _rateService.GetUsdToZarRateAsync();
            return Json(new { success = true, rate, source = "api" });
        }
        catch
        {
            return Json(new { success = false, rate = 18.50m, source = "fallback" });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task PopulateContractDropdown(ServiceRequestViewModel vm)
    {
        var contracts = await _db.Contracts
            .Include(c => c.Client)
            .Where(c => c.Status == ContractStatus.Active)
            .OrderBy(c => c.Client.Name)
            .ToListAsync();

        vm.ContractSelectList = contracts.Select(c => new SelectListItem
        {
            Value    = c.Id.ToString(),
            Text     = $"{c.Client?.Name} — {c.Status} ({c.StartDate:yyyy-MM-dd} → {c.EndDate:yyyy-MM-dd})",
            Selected = c.Id == vm.ContractId
        }).ToList();

        // If editing/prefilling a non-active contract, add it too
        if (vm.ContractId != Guid.Empty && !vm.ContractSelectList.Any(x => x.Value == vm.ContractId.ToString()))
        {
            var existing = await _db.Contracts.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == vm.ContractId);
            if (existing != null)
            {
                vm.ContractSelectList.Insert(0, new SelectListItem
                {
                    Value    = existing.Id.ToString(),
                    Text     = $"{existing.Client?.Name} — {existing.Status} (read-only)",
                    Selected = true
                });
            }
        }
    }
}
