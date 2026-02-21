using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PMS.Web.Data;
using PMS.Web.Models;

namespace PMS.Web.Services;

public class AuditLogService
{
    private readonly PMSDbContext _context;

    public AuditLogService(PMSDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Logs a customer UPDATE action.
    /// Stores a full snapshot of every field in OldValues and NewValues.
    /// ChangedFields lists only the fields whose values actually changed.
    /// </summary>
    public void LogCustomerUpdate(Customer oldCustomer, Customer newCustomer, string? actionBy = null, string? remarks = null)
    {
        if (oldCustomer == null || newCustomer == null)
            throw new ArgumentNullException("Customer records cannot be null");

        if (oldCustomer.CustomerNo != newCustomer.CustomerNo)
            throw new ArgumentException("Customer IDs must match");

        // Build full snapshots — every field, whether changed or not
        var oldSnapshot = BuildSnapshot(oldCustomer);
        var newSnapshot = BuildSnapshot(newCustomer);

        // Identify which fields actually changed
        var changedFields = new List<string>();
        foreach (var key in oldSnapshot.Keys)
        {
            var oldVal = Normalize(oldSnapshot[key]);
            var newVal = Normalize(newSnapshot.ContainsKey(key) ? newSnapshot[key] : null);
            if (oldVal != newVal)
                changedFields.Add(key);
        }

        var auditLog = new CustomerAuditLog
        {
            CustomerID    = oldCustomer.CustomerNo,
            ActionType    = "UPDATE",
            ChangedFields = changedFields.Count > 0 ? string.Join(", ", changedFields) : "None",
            OldValues     = JsonSerializer.Serialize(oldSnapshot),
            NewValues     = JsonSerializer.Serialize(newSnapshot),
            ActionBy      = actionBy ?? "System",
            ActionDate    = DateTime.Now,
            Remarks       = remarks
        };

        _context.CustomerAuditLogs.Add(auditLog);
    }

    /// <summary>
    /// Logs a customer DELETE action.
    /// OldValues contains the full record snapshot. NewValues is null.
    /// </summary>
    public void LogCustomerDelete(Customer customer, string? actionBy = null, string? remarks = null)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        var auditLog = new CustomerAuditLog
        {
            CustomerID    = customer.CustomerNo,
            ActionType    = "DELETE",
            ChangedFields = "ALL",
            OldValues     = JsonSerializer.Serialize(BuildSnapshot(customer)),
            NewValues     = null,
            ActionBy      = actionBy ?? "System",
            ActionDate    = DateTime.Now,
            Remarks       = remarks
        };

        _context.CustomerAuditLogs.Add(auditLog);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a flat key-value snapshot of every tracked Customer field.
    /// Add new fields here if the Customer model is extended.
    /// </summary>
    private static Dictionary<string, object?> BuildSnapshot(Customer c) =>
        new()
        {
            { nameof(Customer.CustomerNo),        c.CustomerNo },
            { nameof(Customer.FullName),           c.FullName },
            { nameof(Customer.FatherName),         c.FatherName },
            { nameof(Customer.Cnic),               c.Cnic },
            { nameof(Customer.ContactNo),          c.ContactNo },
            { nameof(Customer.Email),              c.Email },
            { nameof(Customer.Gender),             c.Gender },
            { nameof(Customer.CreationDate),       c.CreationDate.ToString("yyyy-MM-dd") },
            { nameof(Customer.CreatedBy),          c.CreatedBy },
            { nameof(Customer.PlanNo),             c.PlanNo },
            { nameof(Customer.DealerID),           c.DealerID?.ToString() },
            { nameof(Customer.PresAddress),        c.PresAddress },
            { nameof(Customer.PresCity),           c.PresCity },
            { nameof(Customer.PresCountry),        c.PresCountry },
            { nameof(Customer.PremAddress),        c.PremAddress },
            { nameof(Customer.PremCity),           c.PremCity },
            { nameof(Customer.PremCountry),        c.PremCountry },
            { nameof(Customer.CustomerImage),      c.CustomerImage },
            { nameof(Customer.CustomerAttachment), c.CustomerAttachment },
        };

    /// <summary>
    /// Normalises a value for comparison: null and empty string are treated equally.
    /// </summary>
    private static string Normalize(object? value) =>
        string.IsNullOrEmpty(value?.ToString()) ? string.Empty : value!.ToString()!;
}
