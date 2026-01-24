using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class InventoryDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "UID")]
    public int UID { get; set; }

    [Display(Name = "Project")]
    [StringLength(100)]
    public string? Project { get; set; }

    [Display(Name = "Sub Project")]
    [StringLength(100)]
    public string? SubProject { get; set; }

    [Display(Name = "Sector")]
    [StringLength(50)]
    public string? Sector { get; set; }

    [Display(Name = "Block")]
    [StringLength(50)]
    public string? Block { get; set; }

    [Display(Name = "Street")]
    [StringLength(50)]
    public string? Street { get; set; }

    [Display(Name = "Plot No")]
    [StringLength(50)]
    public string? PlotNo { get; set; }

    [Display(Name = "Category")]
    [StringLength(50)]
    public string? Category { get; set; }

    [Display(Name = "Unit Size")]
    [StringLength(50)]
    public string? UnitSize { get; set; }

    [Display(Name = "Unit Type")]
    [StringLength(50)]
    public string? UnitType { get; set; }

    [Display(Name = "Development Status")]
    [StringLength(50)]
    public string? DevelopmentStatus { get; set; }

    [Display(Name = "Construction Status")]
    [StringLength(50)]
    public string? ConstStatus { get; set; }

    [Display(Name = "Allotment Status")]
    [StringLength(50)]
    public string? AllotmentStatus { get; set; }

    [Display(Name = "Customer No")]
    [StringLength(50)]
    public string? CustomerNo { get; set; }

    [Display(Name = "Allotment Date")]
    [DataType(DataType.Date)]
    public DateTime? AllotmentDate { get; set; }

    [Display(Name = "Allotted By")]
    [StringLength(100)]
    public string? AllottedBy { get; set; }

    [Display(Name = "Floor No")]
    [StringLength(50)]
    public string? FloorNo { get; set; }

    [Display(Name = "Unit No")]
    [StringLength(50)]
    public string? UnitNo { get; set; }

    [Display(Name = "Creation Date")]
    [DataType(DataType.Date)]
    public DateTime? CreationDate { get; set; }
}
