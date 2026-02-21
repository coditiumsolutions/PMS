using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

/// <summary>
/// Dealer entity mapped to dbo.Dealers table.
/// </summary>
public class Dealer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "Dealer ID")]
    public int DealerID { get; set; }

    [Required(ErrorMessage = "Dealership Name is required")]
    [Display(Name = "Dealership Name")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Dealership Name must be between 2 and 500 characters")]
    public string DealershipName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registration Date is required")]
    [Display(Name = "Registration Date")]
    [DataType(DataType.Date)]
    public DateTime RegisterationDate { get; set; }

    [Display(Name = "Status")]
    [Required(ErrorMessage = "Status is required")]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "Membership Type")]
    [Required(ErrorMessage = "Membership Type is required")]
    [StringLength(10)]
    public string MembershipType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Owner Name is required")]
    [Display(Name = "Owner Name")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Owner Name must be between 2 and 500 characters")]
    public string OwnerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Owner CNIC is required")]
    [Display(Name = "Owner CNIC (National ID)")]
    [StringLength(15)]
    [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "National ID must be in format xxxxx-xxxxxxx-x (e.g. 12345-1234567-1)")]
    public string OwnerCNIC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile No is required")]
    [Display(Name = "Mobile No")]
    [StringLength(50)]
    [RegularExpression(@"^[\d\s\-+()]{10,50}$", ErrorMessage = "Enter a valid mobile number (at least 10 digits)")]
    public string MobileNo { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    [StringLength(50)]
    public string? Email { get; set; }

    [Display(Name = "Address")]
    [StringLength(50)]
    public string? Address { get; set; }

    [Display(Name = "Owner Details")]
    public string? OwnerDetails { get; set; }

    [Display(Name = "Details")]
    public string? Details { get; set; }

    [Display(Name = "Incentive Percentage")]
    [Range(0, 100, ErrorMessage = "Incentive Percentage must be between 0 and 100")]
    public double? IncentivePercentage { get; set; } = 5.0;
}
