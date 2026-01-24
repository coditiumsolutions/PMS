using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class Customer
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Uid { get; set; }

    [Required(ErrorMessage = "Customer No is required")]
    [Display(Name = "Customer No")]
    [StringLength(50, ErrorMessage = "Customer No cannot exceed 50 characters")]
    [Key]
    public string CustomerNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required")]
    [Display(Name = "Full Name")]
    [StringLength(200, ErrorMessage = "Full Name cannot exceed 200 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Father Name is required")]
    [Display(Name = "Father Name")]
    [StringLength(200, ErrorMessage = "Father Name cannot exceed 200 characters")]
    public string FatherName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CNIC is required")]
    [Display(Name = "CNIC")]
    [StringLength(20, ErrorMessage = "CNIC cannot exceed 20 characters")]
    public string Cnic { get; set; } = string.Empty;

    [Display(Name = "Contact No")]
    [StringLength(20, ErrorMessage = "Contact No cannot exceed 20 characters")]
    public string? ContactNo { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string? Email { get; set; }

    [Display(Name = "Gender")]
    [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters")]
    public string? Gender { get; set; }

    [Display(Name = "Present Address")]
    [StringLength(500, ErrorMessage = "Present Address cannot exceed 500 characters")]
    public string? PresAddress { get; set; }

    [Display(Name = "Permanent Address")]
    [StringLength(500, ErrorMessage = "Permanent Address cannot exceed 500 characters")]
    public string? PremAddress { get; set; }

    [Display(Name = "Present City")]
    [StringLength(100, ErrorMessage = "Present City cannot exceed 100 characters")]
    public string? PresCity { get; set; }

    [Display(Name = "Permanent City")]
    [StringLength(100, ErrorMessage = "Permanent City cannot exceed 100 characters")]
    public string? PremCity { get; set; }

    [Display(Name = "Present Country")]
    [StringLength(100, ErrorMessage = "Present Country cannot exceed 100 characters")]
    public string? PresCountry { get; set; }

    [Display(Name = "Permanent Country")]
    [StringLength(100, ErrorMessage = "Permanent Country cannot exceed 100 characters")]
    public string? PremCountry { get; set; }

    [Required(ErrorMessage = "Creation Date is required")]
    [Display(Name = "Creation Date")]
    [DataType(DataType.Date)]
    public DateTime CreationDate { get; set; } = DateTime.Today;

    [Display(Name = "Created By")]
    [StringLength(100, ErrorMessage = "Created By cannot exceed 100 characters")]
    public string? CreatedBy { get; set; }
}

