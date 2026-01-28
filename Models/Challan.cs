using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class Challan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "UID")]
    public int uid { get; set; }

    [Display(Name = "Customer No")]
    [StringLength(50)]
    public string? customerno { get; set; }

    [Display(Name = "Deposit Slip No")]
    [StringLength(100)]
    public string? depsoiteslipno { get; set; }

    [Display(Name = "Bank Name")]
    [StringLength(100)]
    public string? bankname { get; set; }

    [Display(Name = "Bank Verified")]
    [StringLength(50)]
    public string? bankverified { get; set; }

    [Display(Name = "Deposit Date")]
    [DataType(DataType.Date)]
    public DateTime? depositdate { get; set; }

    [Display(Name = "Bank Verified Date")]
    [DataType(DataType.Date)]
    public DateTime? bankverifieddate { get; set; }

    [Display(Name = "Deposit Type")]
    [StringLength(50)]
    public string? deposittype { get; set; }

    // SQL column is INT, so map to nullable int to avoid cast issues
    [Display(Name = "Amount Paid")]
    public int? amountpaid { get; set; }

    [Display(Name = "Currency")]
    [StringLength(10)]
    public string? currency { get; set; }

    [Display(Name = "Created By")]
    [StringLength(100)]
    public string? createdby { get; set; }

    [Display(Name = "Verified By")]
    [StringLength(100)]
    public string? verifiedby { get; set; }

    [Display(Name = "Creation Date")]
    [DataType(DataType.Date)]
    public DateTime? creationdate { get; set; }
}
