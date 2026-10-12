using FieldService.Form.Data.Configuration;
using FieldService.Form.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldService.Form.Data;

/// <summary>
/// FormDbContext - Core Database Context
///
/// This context manages the core Form module entities:
/// - Form: Form definitions with event sourcing
/// - FormTenant: Tenant-specific configuration
///
/// This is different from FormDataDbContext which is tenant-specific
/// and contains FormSubmission data per tenant.
///
/// Database: Core Form Database (single instance)
/// Schema: form
/// </summary>
internal sealed class FormDbContext(DbContextOptions<FormDbContext> options) : DbContext(options)
{
    public const string TableName = "form";
    public const string SchemaName = "form";

    /// <summary>
    /// Forms with event sourcing
    /// </summary>
    public DbSet<global::FieldService.Form.Entities.Form> Forms => Set<global::FieldService.Form.Entities.Form>();

    /// <summary>
    /// Tenant-specific configurations for Form module
    /// </summary>
    public DbSet<FormTenant> FormTenants => Set<FormTenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for all entities
        modelBuilder.HasDefaultSchema(SchemaName);

        // Apply configurations using FluentAPI
        modelBuilder.ApplyConfiguration(new FormConfiguration());
        modelBuilder.ApplyConfiguration(new FormEventConfiguration());
        modelBuilder.ApplyConfiguration(new FormTenantConfiguration());

        // Global query filters (soft delete, tenant isolation, etc.)
        // Can be added here if needed

        // Seed data (optional - remove if not needed)
        // SeedData(modelBuilder);
    }

    // Optional: Seed initial data for development
    // private static void SeedData(ModelBuilder modelBuilder)
    // {
    //     // Add seed data here if needed
    // }
}
