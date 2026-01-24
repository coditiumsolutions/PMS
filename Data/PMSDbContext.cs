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
    }
}

