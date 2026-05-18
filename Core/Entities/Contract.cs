using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GLMS.Enterprise.Core.Enums;

namespace GLMS.Enterprise.Core.Entities;

public class Contract
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Required]
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    [StringLength(200)]
    public string? ServiceLevel { get; set; }

    [StringLength(500)]
    public string? PdfFilePath { get; set; }

    [StringLength(255)]
    public string? OriginalPdfFileName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [ForeignKey(nameof(ClientId))]
    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
