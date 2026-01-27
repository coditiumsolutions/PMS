using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class RequestedProperty
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Uid { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Customer No")]
    public string CustomerNo { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Requested Project cannot exceed 20 characters")]
    [Display(Name = "Requested Project")]
    public string? ReqProject { get; set; }

    [StringLength(20, ErrorMessage = "Requested Size cannot exceed 20 characters")]
    [Display(Name = "Requested Size")]
    public string? ReqSize { get; set; }

    [StringLength(20, ErrorMessage = "Requested Category cannot exceed 20 characters")]
    [Display(Name = "Requested Category")]
    public string? ReqCategory { get; set; }

    [StringLength(20, ErrorMessage = "Requested Construction cannot exceed 20 characters")]
    [Display(Name = "Requested Construction")]
    public string? ReqConstruction { get; set; }
}

