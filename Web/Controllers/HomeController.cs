using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GLMS.Enterprise.Web.Models;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Enterprise.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new DashboardViewModel
        {
            TotalClients          = await _db.Clients.CountAsync(),
            TotalContracts        = await _db.Contracts.CountAsync(),
            ActiveContracts       = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Active),
            TotalServiceRequests  = await _db.ServiceRequests.CountAsync(),
            PendingRequests       = await _db.ServiceRequests.CountAsync(r => r.Status == ServiceRequestStatus.Pending),
            RecentContracts       = await _db.Contracts
                                        .Include(c => c.Client)
                                        .OrderByDescending(c => c.CreatedAt)
                                        .Take(5)
                                        .ToListAsync()
        };
        return View(stats);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
