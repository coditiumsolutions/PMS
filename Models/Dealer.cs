using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

/// <summary>
/// Affiliated dealer / agent for the property management system.
/// </summary>
public class Dealer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "Dealer ID")]
    public int DealerID { get; set; }

    [Display(Name = "Dealer Code")]
    [StringLength(50)]
    public string? DealerCode { get; set; }

    [Required(ErrorMessage = "Dealer Name is required")]
    [Display(Name = "Dealer Name")]
    [StringLength(100)]
    public string DealerName { get; set; } = string.Empty;

    [Display(Name = "Company Name")]
    [StringLength(150)]
    public string? CompanyName { get; set; }

    [Display(Name = "Contact No")]
    [StringLength(100)]
    public string? ContactNo { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Display(Name = "Address")]
    [StringLength(255)]
    public string? Address { get; set; }

    [Display(Name = "City")]
    [StringLength(60)]
    public string? City { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created At")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Created By")]
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Display(Name = "Remarks")]
    [StringLength(500)]
    public string? Remarks { get; set; }
}
