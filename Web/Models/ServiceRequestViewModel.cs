using System.ComponentModel.DataAnnotations;
using GLMS.Enterprise.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GLMS.Enterprise.Web.Models;

public class ServiceRequestViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Please select a contract.")]
    [Display(Name = "Contract")]
    public Guid ContractId { get; set; }

    public string? ContractClientName { get; set; }
    public ContractStatus? ContractStatus { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "USD amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    [Display(Name = "Amount (USD)")]
    public decimal AmountUSD { get; set; }

    [Display(Name = "Amount (ZAR)")]
    public decimal AmountZAR { get; set; }

    [Display(Name = "Exchange Rate Used")]
    public decimal ExchangeRateUsed { get; set; }

    [Display(Name = "Status")]
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

    [StringLength(100)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    // Populated for dropdowns
    public List<SelectListItem> ContractSelectList { get; set; } = new();

    // Live rate display
    public decimal CurrentExchangeRate { get; set; }
    public bool RateFromApi { get; set; }
}
