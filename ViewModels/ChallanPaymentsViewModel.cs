using System.Collections.Generic;
using PMS.Web.Models;

namespace PMS.Web.ViewModels;

public class ChallanPaymentsViewModel
{
    public Challan Challan { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public Payment NewPayment { get; set; } = new();
}

