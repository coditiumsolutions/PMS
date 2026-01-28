using Microsoft.EntityFrameworkCore;
using PMS.Web.Models;

namespace PMS.Web.Data;

public class PMSDbContext : DbContext
{
    public PMSDbContext(DbContextOptions<PMSDbContext> options)
        : base(options)
    {
    }

    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAuditLog> CustomerAuditLogs { get; set; }
    public DbSet<InventoryDetail> InventoryDetails { get; set; }
    public DbSet<Challan> Challans { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentPlan> PaymentPlans { get; set; }
    public DbSet<PaymentPlanChild> PaymentPlanChildren { get; set; }
    public DbSet<Transfer> Transfers { get; set; }
    public DbSet<RequestedProperty> RequestedProperties { get; set; }
    public DbSet<Registration> Registrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            // Set CustomerNo as primary key
            entity.HasKey(e => e.CustomerNo);

            // Configure CustomerNo - not auto-generated (user-provided)
            entity.Property(e => e.CustomerNo)
                .ValueGeneratedNever();

            // Configure Uid as identity but not primary key
            entity.Property(e => e.Uid)
                .ValueGeneratedOnAdd();

            // Map property names to actual database column names where they differ
            entity.Property(e => e.Cnic)
                .HasColumnName("CnicNo");

            entity.Property(e => e.CreatedBy)
                .HasColumnName("Createdby");

            // Map to Customers table
            entity.ToTable("Customers");
        });

        // Configure CustomerAuditLog entity
        modelBuilder.Entity<CustomerAuditLog>(entity =>
        {
            entity.HasKey(e => e.LogID);
            entity.ToTable("CustomerAuditLog");
            
            // Configure indexes for better query performance
            entity.HasIndex(e => e.CustomerID);
            entity.HasIndex(e => e.ActionDate);
            entity.HasIndex(e => new { e.CustomerID, e.ActionDate });
            
            // Set default value for ActionDate
            entity.Property(e => e.ActionDate)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure Project entity
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Projects");
            
            // Configure Id as identity
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            // Set default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
            
            // Explicitly map SubProject column
            entity.Property(e => e.SubProject)
                .HasColumnName("SubProject")
                .HasMaxLength(100)
                .IsRequired()
                .HasDefaultValue("MAIN");
            
            // Explicitly map Prefix column
            entity.Property(e => e.Prefix)
                .HasColumnName("Prefix")
                .HasMaxLength(50)
                .IsRequired(false);
        });

        // Configure InventoryDetail entity
        modelBuilder.Entity<InventoryDetail>(entity =>
        {
            entity.HasKey(e => e.UID);
            entity.ToTable("InventoryDetail");
            
            // Configure UID as identity
            entity.Property(e => e.UID)
                .ValueGeneratedOnAdd();
            
            // Set default value for CreationDate
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure Challan entity
        modelBuilder.Entity<Challan>(entity =>
        {
            entity.HasKey(e => e.uid);
            entity.ToTable("Challan");
            
            // Configure uid as identity
            entity.Property(e => e.uid)
                .ValueGeneratedOnAdd();
            
            // Set default value for creationdate
            entity.Property(e => e.creationdate)
                .HasDefaultValueSql("GETDATE()");
        });

        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.uId);
            entity.ToTable("Payments");
            
            // Configure uId as identity
            entity.Property(e => e.uId)
                .ValueGeneratedOnAdd();
        });

        // Configure PaymentPlan entity
        modelBuilder.Entity<PaymentPlan>(entity =>
        {
            entity.HasKey(e => e.uid);
            entity.ToTable("PaymentPlan");

            entity.Property(e => e.uid)
                .ValueGeneratedOnAdd();
        });

        // Configure PaymentPlanChild entity
        modelBuilder.Entity<PaymentPlanChild>(entity =>
        {
            entity.HasKey(e => e.uid);
            entity.ToTable("paymentplanchild");

            entity.Property(e => e.uid)
                .ValueGeneratedOnAdd();
        });

        // Configure Transfer entity
        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.HasKey(e => e.uId);
            entity.ToTable("Transfer");
            
            // Configure uId as identity
            entity.Property(e => e.uId)
                .ValueGeneratedOnAdd();
        });

        // Configure RequestedProperty entity
        modelBuilder.Entity<RequestedProperty>(entity =>
        {
            entity.HasKey(e => e.Uid);
            entity.ToTable("RequestedProperty");

            entity.Property(e => e.Uid)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CustomerNo)
                .HasColumnName("customerno")
                .HasMaxLength(50);

            entity.Property(e => e.ReqProject)
                .HasColumnName("reqproject")
                .HasMaxLength(20);

            entity.Property(e => e.ReqSize)
                .HasColumnName("reqsize")
                .HasMaxLength(20);

            entity.Property(e => e.ReqCategory)
                .HasColumnName("reqcategory")
                .HasMaxLength(20);

            entity.Property(e => e.ReqConstruction)
                .HasColumnName("reqconstruction")
                .HasMaxLength(20);
        });

        // Configure Registration entity
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.RegID);
            entity.ToTable("Registration");
            
            // Configure RegID as identity
            entity.Property(e => e.RegID)
                .ValueGeneratedOnAdd();
            
            // Set default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
            
            entity.Property(e => e.Status)
                .HasDefaultValue("Pending");
            
            // Configure indexes
            entity.HasIndex(e => e.ProjectID);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

