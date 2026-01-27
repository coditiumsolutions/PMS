using PMS.Web.Models;

namespace PMS.Web.ViewModels;

public class CustomerCreateViewModel
{
    public Customer Customer { get; set; } = new();
    public RequestedProperty RequestedProperty { get; set; } = new();
}

