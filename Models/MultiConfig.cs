using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

[Table("MultiValueConfigurations")]
public class MultiConfig
{
    [Key]
    [Column("UId")]
    public int UId { get; set; }

    [Required(ErrorMessage = "Config Key is required")]
    [Display(Name = "Config Key")]
    [StringLength(50, ErrorMessage = "Config Key cannot exceed 50 characters")]
    public string ConfigKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Config Value is required")]
    [Display(Name = "Config Value")]
    [StringLength(100, ErrorMessage = "Config Value cannot exceed 100 characters")]
    public string ConfigValue { get; set; } = string.Empty;
}
