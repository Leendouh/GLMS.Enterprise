using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GLMS.Enterprise.Core.Enums;

namespace GLMS.Enterprise.Core.Entities;

public class ServiceRequest
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountUSD { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountZAR { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal ExchangeRateUsed { get; set; }

    [Required]
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [ForeignKey(nameof(ContractId))]
    public virtual Contract Contract { get; set; } = null!;
}
