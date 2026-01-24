using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "ID")]
    public int uId { get; set; }

    [Display(Name = "Customer No")]
    [StringLength(50)]
    public string customerno { get; set; } = string.Empty;

    [Display(Name = "Paid Amount")]
    [StringLength(255)]
    public string? PaidAmount { get; set; }

    [Display(Name = "Payment Date")]
    [StringLength(60)]
    public string? PaymentDate { get; set; }

    [Display(Name = "Paid Date")]
    [StringLength(60)]
    public string? PaidDate { get; set; }

    [Display(Name = "Method")]
    [StringLength(60)]
    public string? Method { get; set; }

    [Display(Name = "Deposit Slip No")]
    [StringLength(50)]
    public string DepsoiteSlipNo { get; set; } = string.Empty;

    [Display(Name = "Bank Name")]
    [StringLength(50)]
    public string BankName { get; set; } = string.Empty;

    [Display(Name = "DD/Po No")]
    [StringLength(50)]
    public string? DD_PoNo { get; set; }

    [Display(Name = "Installment No")]
    [StringLength(50)]
    public string InstallmentNo { get; set; } = string.Empty;

    [Display(Name = "Payment For")]
    [StringLength(100)]
    public string PaymentFor { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Created At")]
    [StringLength(60)]
    public string CreatedAt { get; set; } = string.Empty;

    [Display(Name = "Created By")]
    [StringLength(100)]
    public string? CreatedBy { get; set; }
}
