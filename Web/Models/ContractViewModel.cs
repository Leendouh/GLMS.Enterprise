using System.ComponentModel.DataAnnotations;
using GLMS.Enterprise.Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GLMS.Enterprise.Web.Models;

public class ContractViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Please select a client.")]
    [Display(Name = "Client")]
    public Guid ClientId { get; set; }

    public string? ClientName { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

    [Required(ErrorMessage = "Status is required.")]
    [Display(Name = "Status")]
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    [StringLength(200)]
    [Display(Name = "Service Level")]
    public string? ServiceLevel { get; set; }

    [Display(Name = "Signed Agreement (PDF)")]
    public IFormFile? PdfFile { get; set; }

    public string? PdfFilePath { get; set; }
    public string? OriginalPdfFileName { get; set; }

    [StringLength(100)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public int ServiceRequestCount { get; set; }

    // Populated for dropdowns
    public List<SelectListItem> ClientSelectList { get; set; } = new();
}

public class ContractSearchViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Start Date From")]
    public DateTime? StartDateFrom { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Start Date To")]
    public DateTime? StartDateTo { get; set; }

    [Display(Name = "Status")]
    public ContractStatus? Status { get; set; }

    public List<ContractViewModel> Results { get; set; } = new();
    public bool HasSearched { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}
