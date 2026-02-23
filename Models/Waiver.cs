using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

[Table("Waivers")]
public class Waiver
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("uid")]
    [Display(Name = "ID")]
    public int Uid { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Customer No")]
    public string CustomerNo { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Project Name")]
    public string? ProjectName { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Waive Off Type")]
    public string WaiveOffType { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Payment Description")]
    public string? PaymentDesc { get; set; }

    [Display(Name = "Installment No")]
    public int? InstallmentNo { get; set; }

    [Display(Name = "Due Amount")]
    public long? DueAmount { get; set; }

    [StringLength(150)]
    [Display(Name = "Property Detail")]
    public string? PropertyDetail { get; set; }

    [StringLength(100)]
    [Display(Name = "Requested Plot Type")]
    public string? RequestedPlotType { get; set; }

    [Display(Name = "Waive Off Amount")]
    public long? WaiveOffAmount { get; set; }

    [Display(Name = "Created Date")]
    public DateTime? CreatedDate { get; set; }

    [StringLength(100)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Is Active")]
    public bool? IsActive { get; set; } = true;
}
