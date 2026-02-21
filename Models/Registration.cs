using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class Registration
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RegID { get; set; }

    [Display(Name = "Full Name")]
    [StringLength(150)]
    public string? FullName { get; set; }

    [Display(Name = "CNIC")]
    [StringLength(50)]
    public string? CNIC { get; set; }

    [Display(Name = "Phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Display(Name = "Contact No 1")]
    [StringLength(50)]
    public string? ContactNo1 { get; set; }

    [Display(Name = "Contact No 2")]
    [StringLength(50)]
    public string? ContactNo2 { get; set; }

    [Display(Name = "Email")]
    [StringLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Requested Project")]
    public int? ProjectID { get; set; }

    [ForeignKey("ProjectID")]
    public Project? Project { get; set; }

    [Display(Name = "Requested Size")]
    [StringLength(100)]
    public string? RequestedSize { get; set; }

    [Display(Name = "Mailing Address")]
    [StringLength(250)]
    public string? MailingAddress { get; set; }

    [Display(Name = "Mailing City")]
    [StringLength(100)]
    public string? MailingCity { get; set; }

    [Display(Name = "Mailing Country")]
    [StringLength(100)]
    public string? MailingCountry { get; set; }

    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }

    [Display(Name = "Created At")]
    public DateTime? CreatedAt { get; set; }

    [Display(Name = "Status")]
    [StringLength(50)]
    public string? Status { get; set; }
}
