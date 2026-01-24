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

    [Display(Name = "Paid Date")]
    [StringLength(60)]
    public string? PaidDate { get; set; }

    [Display(Name = "Method")]
    [StringLength(60)]
    public string? Method { get; set; }

    [Display(Name = "Bank Name")]
    [StringLength(50)]
    public string BankName { get; set; } = string.Empty;

    [Display(Name = "Created By")]
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Display(Name = "DS No")]
    [StringLength(50)]
    public string? DSNo { get; set; }

    [Display(Name = "DS Date")]
    [StringLength(60)]
    public string? DSDate { get; set; }

    [Display(Name = "DD No")]
    [StringLength(50)]
    public string? DDNo { get; set; }

    [Display(Name = "DD Date")]
    [StringLength(60)]
    public string? DDDate { get; set; }

    [Display(Name = "Cheque No")]
    [StringLength(50)]
    public string? ChequeNo { get; set; }

    [Display(Name = "Cheque Date")]
    [StringLength(60)]
    public string? ChequeDate { get; set; }

    [Display(Name = "Install No")]
    [StringLength(50)]
    public string? InstallNo { get; set; }

    [Display(Name = "Payment Description")]
    [StringLength(500)]
    public string? PaymentDescription { get; set; }

    [Display(Name = "Created On")]
    [StringLength(60)]
    public string CreatedOn { get; set; } = string.Empty;
}
