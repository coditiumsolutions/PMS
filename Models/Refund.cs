using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

[Table("Refunds")]
public class Refund
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("uid")]
    [Display(Name = "ID")]
    public int Uid { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Refund Type")]
    public string RefundType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Customer No")]
    public string CustomerNo { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Father Name")]
    public string? FatherName { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "CNIC No")]
    public string CNICNo { get; set; } = string.Empty;

    [Display(Name = "Total Paid Amount")]
    public long TotalPaidAmount { get; set; }

    [Display(Name = "Refundable Amount")]
    public long RefundableAmount { get; set; }

    [Display(Name = "Deduction")]
    public long Deduction { get; set; }

    [Display(Name = "Amount To Refund")]
    public long AmountToRefund { get; set; }

    [StringLength(500)]
    [Display(Name = "Detail")]
    public string? Detail { get; set; }

    [StringLength(150)]
    [Display(Name = "Project Name")]
    public string? ProjectName { get; set; }

    [StringLength(50)]
    [Display(Name = "Application No")]
    public string? ApplicationNo { get; set; }

    [StringLength(50)]
    [Display(Name = "Plot Size")]
    public string? PlotSize { get; set; }

    [StringLength(100)]
    [Display(Name = "Property Detail")]
    public string? PropertyDetail { get; set; }

    [Display(Name = "Created Date")]
    public DateTime? CreatedDate { get; set; }

    [StringLength(100)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Is Active")]
    public bool? IsActive { get; set; } = true;
}
