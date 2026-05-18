using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Enterprise.Web.Controllers;

public class ClientController : Controller
{
    private readonly ApplicationDbContext _db;

    public ClientController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Client
    public async Task<IActionResult> Index()
    {
        var clients = await _db.Clients
            .Include(c => c.Contracts)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(clients);
    }

    // GET: /Client/Details/id
    public async Task<IActionResult> Details(Guid id)
    {
        var client = await _db.Clients
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client == null) return NotFound();
        return View(client);
    }

    // GET: /Client/Create
    public IActionResult Create() => View(new ClientViewModel());

    // POST: /Client/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientViewModel model)
    {
        // Check for duplicate name
        if (await _db.Clients.AnyAsync(c => c.Name == model.Name))
            ModelState.AddModelError(nameof(model.Name), "A client with this name already exists.");

        if (!ModelState.IsValid) return View(model);

        var client = new Client
        {
            Id           = Guid.NewGuid(),
            Name         = model.Name,
            ContactEmail = model.ContactEmail,
            ContactPhone = model.ContactPhone,
            Region       = model.Region,
            Address      = model.Address,
            CreatedAt    = DateTime.UtcNow
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Client '{client.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Client/Edit/id
    public async Task<IActionResult> Edit(Guid id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        var vm = new ClientViewModel
        {
            Id           = client.Id,
            Name         = client.Name,
            ContactEmail = client.ContactEmail,
            ContactPhone = client.ContactPhone,
            Region       = client.Region,
            Address      = client.Address,
            CreatedAt    = client.CreatedAt
        };
        return View(vm);
    }

    // POST: /Client/Edit/id
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ClientViewModel model)
    {
        if (id != model.Id) return BadRequest();

        // Duplicate name check (exclude self)
        if (await _db.Clients.AnyAsync(c => c.Name == model.Name && c.Id != id))
            ModelState.AddModelError(nameof(model.Name), "A client with this name already exists.");

        if (!ModelState.IsValid) return View(model);

        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        client.Name         = model.Name;
        client.ContactEmail = model.ContactEmail;
        client.ContactPhone = model.ContactPhone;
        client.Region       = model.Region;
        client.Address      = model.Address;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Client '{client.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Client/Delete/id
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = await _db.Clients
            .Include(c => c.Contracts)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client == null) return NotFound();
        return View(client);
    }

    // POST: /Client/Delete/id
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var client = await _db.Clients.Include(c => c.Contracts).FirstOrDefaultAsync(c => c.Id == id);
        if (client == null) return NotFound();

        if (client.Contracts.Any())
        {
            TempData["Error"] = "Cannot delete a client that has contracts. Remove contracts first.";
            return RedirectToAction(nameof(Index));
        }

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Client deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
