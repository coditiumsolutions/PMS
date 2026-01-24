using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class CustomerAuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogID { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Customer ID")]
    public string CustomerID { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Display(Name = "Action Type")]
    public string ActionType { get; set; } = string.Empty; // 'UPDATE' or 'DELETE'

    [Display(Name = "Changed Fields")]
    public string? ChangedFields { get; set; } // Comma-separated list or JSON

    [Display(Name = "Old Values")]
    public string? OldValues { get; set; } // JSON format

    [Display(Name = "New Values")]
    public string? NewValues { get; set; } // JSON format (NULL for DELETE)

    [Required]
    [StringLength(50)]
    [Display(Name = "Action By")]
    public string ActionBy { get; set; } = "System"; // Logged-in username / user ID

    [Required]
    [Display(Name = "Action Date")]
    [DataType(DataType.DateTime)]
    public DateTime ActionDate { get; set; } = DateTime.Now;

    [StringLength(500)]
    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }
}

