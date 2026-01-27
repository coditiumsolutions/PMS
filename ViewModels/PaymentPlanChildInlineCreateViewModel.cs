using System.ComponentModel.DataAnnotations;

namespace PMS.Web.ViewModels;

public class PaymentPlanChildInlineCreateViewModel
{
    [Required]
    public string Planno { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Payment Description")]
    public string PaymentDesc { get; set; } = string.Empty;

    [Display(Name = "Installment No")]
    public string? InstallmentNo { get; set; }

    [Required]
    [Display(Name = "Due Date")]
    public string DueDate { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Due Amount")]
    public string DueAmount { get; set; } = string.Empty;
}

