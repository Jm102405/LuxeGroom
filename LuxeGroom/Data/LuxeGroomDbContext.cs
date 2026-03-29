/*
 * LuxeGroomDbContext.cs
 * Entity Framework Core DbContext for LuxeGroom.
 * Manages database connection and entity mappings for User, Reservation, and Customer tables.
 * Updated in Thread 2.7: Added Customer DbSet and model configuration.
 */

using LuxeGroom.Data.Generated;
using Microsoft.EntityFrameworkCore;

namespace LuxeGroom.Data;

public partial class LuxeGroomDbContext : DbContext
{
    // Parameterless constructor for design-time tooling
    public LuxeGroomDbContext()
    {
    }

    // Constructor used at runtime with injected EF Core options
    public LuxeGroomDbContext(DbContextOptions<LuxeGroomDbContext> options)
        : base(options)
    {
    }

    // DbSet for the Reservations table
    public virtual DbSet<Reservation> Reservations { get; set; }

    // DbSet for the Users table
    public virtual DbSet<User> Users { get; set; }

    // DbSet for the Customers table
    public virtual DbSet<Customer> Customers { get; set; }

    // Configure entity keys and default values via Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure primary key name for the Reservations table
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reservations__id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            // Configure primary key name for the Users table
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACABFAE32C");

            // Default DateCreated to the current SQL Server datetime
            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            // Default Status to Active on new user creation
            entity.Property(e => e.Status).HasDefaultValue("Active");
        });

        // Configure primary key for the Customers table
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customers__CustomerId");

            // Default CreatedAt to the current SQL Server datetime
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        // Hook for additional partial model configuration
        OnModelCreatingPartial(modelBuilder);
    }

    // Partial method for extending model configuration in another file
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
