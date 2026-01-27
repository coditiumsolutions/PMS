using System.Collections.Generic;

namespace PMS.Web.ViewModels;

public class InventoryBulkImportViewModel
{
    public int Inserted { get; set; }
    public int Duplicates { get; set; }
    public int Failed { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public int Processed => Inserted + Duplicates + Failed;
}
