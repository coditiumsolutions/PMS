using System.ComponentModel.DataAnnotations;

namespace PMS.Web.Models;

public class Configuration
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Config Key is required")]
    [Display(Name = "Config Key")]
    [StringLength(100, ErrorMessage = "Config Key cannot exceed 100 characters")]
    public string ConfigKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Config Value is required")]
    [Display(Name = "Config Value")]
    [StringLength(500, ErrorMessage = "Config Value cannot exceed 500 characters")]
    public string ConfigValue { get; set; } = string.Empty;

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
}
