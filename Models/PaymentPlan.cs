using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class PaymentPlan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "UID")]
    public int uid { get; set; }

    [Display(Name = "Plan No")]
    [StringLength(100)]
    public string? planno { get; set; }

    [Display(Name = "Plan Detail")]
    [StringLength(50)]
    public string? plandetail { get; set; }

    [Display(Name = "Total Amount")]
    [StringLength(255)]
    public string? totalamount { get; set; }

    [Display(Name = "Payment Type")]
    [StringLength(60)]
    public string? paymenttype { get; set; }

    [Display(Name = "Comments")]
    public string? comments { get; set; }

    [Display(Name = "Created By")]
    [StringLength(100)]
    public string? createdby { get; set; }

    [Display(Name = "Creation Date")]
    [DataType(DataType.Date)]
    public DateTime? creationdate { get; set; }

    [Display(Name = "Size Detail")]
    [StringLength(50)]
    public string? sizedetail { get; set; }
}
