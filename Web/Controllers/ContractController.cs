using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Services;
using GLMS.Enterprise.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Enterprise.Web.Controllers;

public class ContractController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IContractRepository  _contractRepo;
    private readonly IContractService     _contractService;
    private readonly IFileService         _fileService;
    private readonly IWebHostEnvironment  _env;

    public ContractController(
        ApplicationDbContext db,
        IContractRepository  contractRepo,
        IContractService     contractService,
        IFileService         fileService,
        IWebHostEnvironment  env)
    {
        _db              = db;
        _contractRepo    = contractRepo;
        _contractService = contractService;
        _fileService     = fileService;
        _env             = env;
    }

    private string UploadFolder =>
        Path.Combine(_env.WebRootPath, "Uploads", "Contracts");

    // ── Index ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var contracts = await _contractRepo.GetAllAsync();
        return View(contracts);
    }

    // ── Search / Filter ───────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Search(
        DateTime? startDateFrom,
        DateTime? startDateTo,
        ContractStatus? status,
        int page = 1,
        int pageSize = 10)
    {
        var vm = new ContractSearchViewModel
        {
            StartDateFrom = startDateFrom,
            StartDateTo   = startDateTo,
            Status        = status,
            Page          = page,
            PageSize      = pageSize,
            HasSearched   = Request.Query.Count > 0
        };

        if (vm.HasSearched)
        {
            var paged = await _contractRepo.SearchAsync(startDateFrom, startDateTo, status, page, pageSize);
            vm.TotalCount = paged.TotalCount;
            vm.Page = paged.Page;
            vm.PageSize = paged.PageSize;
            vm.Results = paged.Items.Select(c => new ContractViewModel
            {
                Id                  = c.Id,
                ClientId            = c.ClientId,
                ClientName          = c.Client?.Name,
                StartDate           = c.StartDate,
                EndDate             = c.EndDate,
                Status              = c.Status,
                ServiceLevel        = c.ServiceLevel,
                PdfFilePath         = c.PdfFilePath,
                OriginalPdfFileName = c.OriginalPdfFileName,
                CreatedAt           = c.CreatedAt,
                CreatedBy           = c.CreatedBy,
                ServiceRequestCount = c.ServiceRequests?.Count ?? 0
            }).ToList();
        }

        return View(vm);
    }

    // ── Details ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(Guid id)
    {
        var contract = await _contractRepo.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return View(contract);
    }

    // ── Create GET ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Create()
    {
        var vm = new ContractViewModel
        {
            StartDate = DateTime.Today,
            EndDate   = DateTime.Today.AddYears(1)
        };
        await PopulateClientDropdown(vm);
        return View(vm);
    }

    // ── Create POST ───────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContractViewModel model)
    {
        // Server-side date validation
        if (model.EndDate <= model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");

        // PDF is required on create
        if (model.PdfFile == null || model.PdfFile.Length == 0)
            ModelState.AddModelError(nameof(model.PdfFile), "A signed PDF agreement is required.");
        else if (!_fileService.ValidatePdf(model.PdfFile))
            ModelState.AddModelError(nameof(model.PdfFile), "Invalid file. Only PDF files up to 10 MB are accepted.");

        if (!ModelState.IsValid)
        {
            await PopulateClientDropdown(model);
            return View(model);
        }

        // Save PDF
        var (saved, filePath, fileError) = await _fileService.SavePdfAsync(model.PdfFile!, UploadFolder);
        if (!saved)
        {
            ModelState.AddModelError(nameof(model.PdfFile), fileError);
            await PopulateClientDropdown(model);
            return View(model);
        }

        var contract = new Contract
        {
            Id                  = Guid.NewGuid(),
            ClientId            = model.ClientId,
            StartDate           = model.StartDate,
            EndDate             = model.EndDate,
            Status              = model.Status,
            ServiceLevel        = model.ServiceLevel,
            PdfFilePath         = filePath,
            OriginalPdfFileName = model.PdfFile!.FileName,
            CreatedBy           = model.CreatedBy ?? "System",
            CreatedAt           = DateTime.UtcNow
        };

        await _contractRepo.AddAsync(contract);
        TempData["Success"] = "Contract created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit GET ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Edit(Guid id)
    {
        var contract = await _contractRepo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        var vm = MapToViewModel(contract);
        await PopulateClientDropdown(vm);
        return View(vm);
    }

    // ── Edit POST ─────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ContractViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (model.EndDate <= model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date must be after start date.");

        // Validate new PDF only if one was supplied
        if (model.PdfFile != null && model.PdfFile.Length > 0)
        {
            if (!_fileService.ValidatePdf(model.PdfFile))
                ModelState.AddModelError(nameof(model.PdfFile), "Invalid file. Only PDF files up to 10 MB are accepted.");
        }

        // Validate status transition
        var transitionResult = await _contractService.ValidateContractStatusTransitionAsync(id, model.Status);
        if (!transitionResult.IsValid)
        {
            // Only block if the status actually changed — fetch current
            var existing = await _contractRepo.GetByIdAsync(id);
            if (existing != null && existing.Status != model.Status)
                ModelState.AddModelError(nameof(model.Status), transitionResult.ErrorMessage);
        }

        if (!ModelState.IsValid)
        {
            await PopulateClientDropdown(model);
            return View(model);
        }

        var contract = await _contractRepo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        // Replace PDF if a new one was uploaded
        if (model.PdfFile != null && model.PdfFile.Length > 0)
        {
            // Delete old file
            if (!string.IsNullOrWhiteSpace(contract.PdfFilePath))
                _fileService.DeleteFile(Path.Combine(UploadFolder, contract.PdfFilePath));

            var (saved, filePath, fileError) = await _fileService.SavePdfAsync(model.PdfFile, UploadFolder);
            if (!saved)
            {
                ModelState.AddModelError(nameof(model.PdfFile), fileError);
                await PopulateClientDropdown(model);
                return View(model);
            }
            contract.PdfFilePath         = filePath;
            contract.OriginalPdfFileName = model.PdfFile.FileName;
        }

        contract.ClientId    = model.ClientId;
        contract.StartDate   = model.StartDate;
        contract.EndDate     = model.EndDate;
        contract.Status      = model.Status;
        contract.ServiceLevel = model.ServiceLevel;

        await _contractRepo.UpdateAsync(contract);
        TempData["Success"] = "Contract updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Delete(Guid id)
    {
        var contract = await _contractRepo.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return View(contract);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var contract = await _contractRepo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        if (contract.ServiceRequests.Any())
        {
            TempData["Error"] = "Cannot delete a contract that has service requests.";
            return RedirectToAction(nameof(Index));
        }

        // Remove associated PDF
        if (!string.IsNullOrWhiteSpace(contract.PdfFilePath))
            _fileService.DeleteFile(Path.Combine(UploadFolder, contract.PdfFilePath));

        await _contractRepo.DeleteAsync(id);
        TempData["Success"] = "Contract deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Download PDF ──────────────────────────────────────────────────────────
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var contract = await _contractRepo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        if (string.IsNullOrWhiteSpace(contract.PdfFilePath))
            return NotFound("No PDF associated with this contract.");

        var fullPath = Path.Combine(UploadFolder, contract.PdfFilePath);
        if (!System.IO.File.Exists(fullPath))
            return NotFound("PDF file not found on server.");

        var fileName = contract.OriginalPdfFileName ?? "contract.pdf";
        return PhysicalFile(fullPath, "application/pdf", fileName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task PopulateClientDropdown(ContractViewModel vm)
    {
        var clients = await _db.Clients.OrderBy(c => c.Name).ToListAsync();
        vm.ClientSelectList = clients.Select(c => new SelectListItem
        {
            Value    = c.Id.ToString(),
            Text     = c.Name,
            Selected = c.Id == vm.ClientId
        }).ToList();
    }

    private static ContractViewModel MapToViewModel(Contract c) => new()
    {
        Id                  = c.Id,
        ClientId            = c.ClientId,
        ClientName          = c.Client?.Name,
        StartDate           = c.StartDate,
        EndDate             = c.EndDate,
        Status              = c.Status,
        ServiceLevel        = c.ServiceLevel,
        PdfFilePath         = c.PdfFilePath,
        OriginalPdfFileName = c.OriginalPdfFileName,
        CreatedBy           = c.CreatedBy,
        CreatedAt           = c.CreatedAt,
        ServiceRequestCount = c.ServiceRequests?.Count ?? 0
    };
}
