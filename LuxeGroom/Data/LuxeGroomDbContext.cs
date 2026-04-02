/*
 * LuxeGroomDbContext.cs
 * Entity Framework Core DbContext for LuxeGroom.
 * Manages database connection and entity mappings for User, Reservation, Customer, and Payment tables.
 * Updated in Thread 2.7: Added Customer DbSet and model configuration.
 * Updated in Thread 3.6: Added Customer → Reservations relationship (HasMany/WithOne).
 * Updated in Thread 3.7: Added Payment DbSet + fixed Customer DateCreated column mapping.
 */

using LuxeGroom.Data.Generated;
using Microsoft.EntityFrameworkCore;

namespace LuxeGroom.Data;

public partial class LuxeGroomDbContext : DbContext
{
    public LuxeGroomDbContext()
    {
    }

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

    // DbSet for the Payments table — NEW (Thread 3.7)
    public virtual DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reservations__id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACABFAE32C");
            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("Active");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customers__CustomerId");

            // Fixed: DateCreated (matches column: date_created)
            entity.Property(e => e.DateCreated).HasDefaultValueSql("(getdate())");

            // One customer → many reservations
            entity.HasMany(e => e.Reservations)
                  .WithOne(r => r.Customer)
                  .HasForeignKey(r => r.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Payment entity configuration — NEW (Thread 3.7)
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payments__id");

            // Default status to Unpaid
            entity.Property(e => e.Status).HasDefaultValue("Unpaid");

            // Payment → Reservation relationship
            entity.HasOne(e => e.Reservation)
                  .WithMany()
                  .HasForeignKey(e => e.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}