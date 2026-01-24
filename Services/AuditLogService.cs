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
    /// Logs a customer UPDATE action by comparing old and new values
    /// Note: This method does NOT call SaveChangesAsync - it should be called within a transaction
    /// </summary>
    public void LogCustomerUpdate(Customer oldCustomer, Customer newCustomer, string? actionBy = null, string? remarks = null)
    {
        if (oldCustomer == null || newCustomer == null)
        {
            throw new ArgumentNullException("Customer records cannot be null");
        }

        if (oldCustomer.CustomerNo != newCustomer.CustomerNo)
        {
            throw new ArgumentException("Customer IDs must match");
        }

        // Compare fields and identify changes
        var changedFields = new List<string>();
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        // Compare each property
        CompareProperty(nameof(Customer.FullName), oldCustomer.FullName, newCustomer.FullName, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.FatherName), oldCustomer.FatherName, newCustomer.FatherName, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.Cnic), oldCustomer.Cnic, newCustomer.Cnic, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.ContactNo), oldCustomer.ContactNo, newCustomer.ContactNo, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.Email), oldCustomer.Email, newCustomer.Email, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.Gender), oldCustomer.Gender, newCustomer.Gender, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.PresAddress), oldCustomer.PresAddress, newCustomer.PresAddress, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.PremAddress), oldCustomer.PremAddress, newCustomer.PremAddress, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.PresCity), oldCustomer.PresCity, newCustomer.PresCity, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.PremCity), oldCustomer.PremCity, newCustomer.PremCity, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.PresCountry), oldCustomer.PresCountry, newCustomer.PresCountry, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.PremCountry), oldCustomer.PremCountry, newCustomer.PremCountry, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.CreationDate), oldCustomer.CreationDate, newCustomer.CreationDate, changedFields, oldValues, newValues);
        CompareProperty(nameof(Customer.CreatedBy), oldCustomer.CreatedBy, newCustomer.CreatedBy, changedFields, oldValues, newValues);

        // Always log the update (even if no fields changed, to maintain audit trail)
        var auditLog = new CustomerAuditLog
        {
            CustomerID = oldCustomer.CustomerNo,
            ActionType = "UPDATE",
            ChangedFields = changedFields.Count > 0 ? string.Join(", ", changedFields) : "None",
            OldValues = JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(newValues),
            ActionBy = actionBy ?? "System",
            ActionDate = DateTime.Now,
            Remarks = remarks
        };

        _context.CustomerAuditLogs.Add(auditLog);
    }

    /// <summary>
    /// Logs a customer DELETE action
    /// Note: This method does NOT call SaveChangesAsync - it should be called within a transaction
    /// </summary>
    public void LogCustomerDelete(Customer customer, string? actionBy = null, string? remarks = null)
    {
        if (customer == null)
        {
            throw new ArgumentNullException(nameof(customer));
        }

        // Serialize the entire customer record
        var customerData = new Dictionary<string, object?>
        {
            { nameof(Customer.CustomerNo), customer.CustomerNo },
            { nameof(Customer.FullName), customer.FullName },
            { nameof(Customer.FatherName), customer.FatherName },
            { nameof(Customer.Cnic), customer.Cnic },
            { nameof(Customer.ContactNo), customer.ContactNo },
            { nameof(Customer.Email), customer.Email },
            { nameof(Customer.Gender), customer.Gender },
            { nameof(Customer.PresAddress), customer.PresAddress },
            { nameof(Customer.PremAddress), customer.PremAddress },
            { nameof(Customer.PresCity), customer.PresCity },
            { nameof(Customer.PremCity), customer.PremCity },
            { nameof(Customer.PresCountry), customer.PresCountry },
            { nameof(Customer.PremCountry), customer.PremCountry },
            { nameof(Customer.CreationDate), customer.CreationDate },
            { nameof(Customer.CreatedBy), customer.CreatedBy }
        };

        var auditLog = new CustomerAuditLog
        {
            CustomerID = customer.CustomerNo,
            ActionType = "DELETE",
            ChangedFields = "ALL", // All fields are deleted
            OldValues = JsonSerializer.Serialize(customerData),
            NewValues = null, // NULL for DELETE
            ActionBy = actionBy ?? "System",
            ActionDate = DateTime.Now,
            Remarks = remarks
        };

        _context.CustomerAuditLogs.Add(auditLog);
    }

    /// <summary>
    /// Helper method to compare property values and track changes
    /// </summary>
    private void CompareProperty(string propertyName, object? oldValue, object? newValue, 
        List<string> changedFields, Dictionary<string, object?> oldValues, Dictionary<string, object?> newValues)
    {
        // Handle DateTime comparison
        if (oldValue is DateTime oldDate && newValue is DateTime newDate)
        {
            if (oldDate != newDate)
            {
                changedFields.Add(propertyName);
                oldValues[propertyName] = oldDate;
                newValues[propertyName] = newDate;
            }
            return;
        }

        // Handle string comparison (including null)
        var oldStr = oldValue?.ToString() ?? string.Empty;
        var newStr = newValue?.ToString() ?? string.Empty;

        if (oldStr != newStr)
        {
            changedFields.Add(propertyName);
            oldValues[propertyName] = oldValue;
            newValues[propertyName] = newValue;
        }
    }
}

