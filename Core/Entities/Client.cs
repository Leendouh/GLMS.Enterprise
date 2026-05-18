using System.ComponentModel.DataAnnotations;

namespace GLMS.Enterprise.Core.Entities;

public class Client
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
