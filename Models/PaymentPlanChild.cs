using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class PaymentPlanChild
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "UID")]
    public int uid { get; set; }

    [Display(Name = "Plan No")]
    [StringLength(100)]
    public string? planno { get; set; }

    [Display(Name = "Payment Description")]
    [StringLength(100)]
    public string? paymentdesc { get; set; }

    [Display(Name = "Installment No")]
    [StringLength(60)]
    public string? installmentno { get; set; }

    [Display(Name = "Due Date")]
    [StringLength(60)]
    public string? duedate { get; set; }

    [Display(Name = "Due Amount")]
    [StringLength(255)]
    public string? dueamount { get; set; }

    [Display(Name = "Surcharge Policy")]
    [StringLength(20)]
    public string? surchargepolicy { get; set; }

    [Display(Name = "Surcharge Rate")]
    [StringLength(50)]
    public string? surchargerate { get; set; }

    [Display(Name = "Discount")]
    [StringLength(50)]
    public string? discount { get; set; }

    [Display(Name = "Comments")]
    public string? comments { get; set; }

    [Display(Name = "Created By")]
    [StringLength(50)]
    public string? createdby { get; set; }

    [Display(Name = "Creation Date")]
    [StringLength(60)]
    public string? creationdate { get; set; }
}
