using System.Collections.Generic;
using PMS.Web.Models;

namespace PMS.Web.ViewModels;

public class PaymentPlanScheduleViewModel
{
    public PaymentPlan Plan { get; set; } = new();
    public List<PaymentPlanChild> Children { get; set; } = new();
}

