using System.ComponentModel.DataAnnotations;

namespace PMS.Web.Models;

public class Project
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Project Name is required")]
    [Display(Name = "Project Name")]
    [StringLength(100, ErrorMessage = "Project Name cannot exceed 100 characters")]
    public string ProjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Project Description is required")]
    [Display(Name = "Project Description")]
    [StringLength(500, ErrorMessage = "Project Description cannot exceed 500 characters")]
    public string ProjectDescription { get; set; } = string.Empty;

    [Display(Name = "Created At")]
    public DateTime? CreatedAt { get; set; }

    [Required(ErrorMessage = "Sub Project is required")]
    [Display(Name = "Sub Project")]
    [StringLength(100, ErrorMessage = "Sub Project cannot exceed 100 characters")]
    public string SubProject { get; set; } = "MAIN";

    [Display(Name = "Prefix")]
    [StringLength(50, ErrorMessage = "Prefix cannot exceed 50 characters")]
    public string? Prefix { get; set; }
}

