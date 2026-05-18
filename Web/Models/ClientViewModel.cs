using System.ComponentModel.DataAnnotations;

namespace GLMS.Enterprise.Web.Models;

public class ClientViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Client name is required.")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    [Display(Name = "Client Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(256)]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number.")]
    [StringLength(50)]
    [Display(Name = "Contact Phone")]
    public string? ContactPhone { get; set; }

    [StringLength(100)]
    [Display(Name = "Region")]
    public string? Region { get; set; }

    [StringLength(500)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }
    public int ContractCount { get; set; }
}
