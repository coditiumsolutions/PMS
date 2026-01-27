using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMS.Web.Models;

public class Transfer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "Transfer ID")]
    public int uId { get; set; }

    [Required(ErrorMessage = "Customer No is required")]
    [Display(Name = "Customer No")]
    [StringLength(50, ErrorMessage = "Customer No cannot exceed 50 characters")]
    public string CustomerNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transfer Type is required")]
    [Display(Name = "Transfer Type")]
    [StringLength(50, ErrorMessage = "Transfer Type cannot exceed 50 characters")]
    public string TransferType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nature of Transfer is required")]
    [Display(Name = "Nature of Transfer")]
    [StringLength(50, ErrorMessage = "Nature of Transfer cannot exceed 50 characters")]
    public string NatureOfTransfer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Initiating Office is required")]
    [Display(Name = "Initiating Office")]
    [StringLength(50, ErrorMessage = "Initiating Office cannot exceed 50 characters")]
    public string InitiatingOffice { get; set; } = string.Empty;

    [Required(ErrorMessage = "Owner Type is required")]
    [Display(Name = "Owner Type")]
    [StringLength(50, ErrorMessage = "Owner Type cannot exceed 50 characters")]
    public string OwnerType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Buyer Name is required")]
    [Display(Name = "Buyer Name")]
    [StringLength(50, ErrorMessage = "Buyer Name cannot exceed 50 characters")]
    public string BuyerName { get; set; } = string.Empty;

    [Display(Name = "Buyer Father Name")]
    [StringLength(50, ErrorMessage = "Buyer Father Name cannot exceed 50 characters")]
    public string? BuyerFatherName { get; set; }

    [Required(ErrorMessage = "Buyer CNIC is required")]
    [Display(Name = "Buyer CNIC")]
    [StringLength(50, ErrorMessage = "Buyer CNIC cannot exceed 50 characters")]
    public string BuyerCnic { get; set; } = string.Empty;

    [Display(Name = "Buyer Contact No")]
    [StringLength(50, ErrorMessage = "Buyer Contact No cannot exceed 50 characters")]
    public string? BuyerContactNo { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Buyer Email")]
    [StringLength(50, ErrorMessage = "Buyer Email cannot exceed 50 characters")]
    public string? BuyerEmail { get; set; }

    [Display(Name = "Buyer Gender")]
    [StringLength(10, ErrorMessage = "Buyer Gender cannot exceed 10 characters")]
    public string? BuyerGender { get; set; }

    [Display(Name = "Buyer Present Address")]
    [StringLength(60, ErrorMessage = "Buyer Present Address cannot exceed 60 characters")]
    public string? BuyerPresAddress { get; set; }

    [Display(Name = "Buyer Permanent Address")]
    [StringLength(60, ErrorMessage = "Buyer Permanent Address cannot exceed 60 characters")]
    public string? BuyerPremAddress { get; set; }

    [Display(Name = "Buyer Present City")]
    [StringLength(60, ErrorMessage = "Buyer Present City cannot exceed 60 characters")]
    public string? BuyerPresCity { get; set; }

    [Display(Name = "Buyer Permanent City")]
    [StringLength(60, ErrorMessage = "Buyer Permanent City cannot exceed 60 characters")]
    public string? BuyerPremCity { get; set; }

    [Display(Name = "Buyer Present Country")]
    [StringLength(60, ErrorMessage = "Buyer Present Country cannot exceed 60 characters")]
    public string? BuyerPresCountry { get; set; }

    [Display(Name = "Buyer Permanent Country")]
    [StringLength(60, ErrorMessage = "Buyer Permanent Country cannot exceed 60 characters")]
    public string? BuyerPremCountry { get; set; }

    [Display(Name = "Creation Date")]
    [StringLength(50, ErrorMessage = "Creation Date cannot exceed 50 characters")]
    public string? CreationDate { get; set; }

    [Display(Name = "Created By")]
    [StringLength(50, ErrorMessage = "Created By cannot exceed 50 characters")]
    public string? CreatedBy { get; set; }

    [Required(ErrorMessage = "Seller Info is required")]
    [Display(Name = "Seller Info")]
    [StringLength(255, ErrorMessage = "Seller Info cannot exceed 255 characters")]
    public string SellerInfo { get; set; } = string.Empty;

    [Display(Name = "NDC Exist")]
    [StringLength(10, ErrorMessage = "NDC Exist cannot exceed 10 characters")]
    public string? NdcExist { get; set; }

    [Display(Name = "NDC Amount")]
    [StringLength(50, ErrorMessage = "NDC Amount cannot exceed 50 characters")]
    public string? NdcAmount { get; set; }

    [Display(Name = "NDC Detail")]
    [StringLength(100, ErrorMessage = "NDC Detail cannot exceed 100 characters")]
    public string? NdcDetail { get; set; }
}
