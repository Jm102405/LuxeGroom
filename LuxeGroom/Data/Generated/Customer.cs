/*
 * Customer.cs
 * EF Core entity for the Customers table in LuxeGroom.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuxeGroom.Data.Generated;

[Table("Customers")]
public partial class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("customer_id")]
    [StringLength(20)]
    public string CustomerId { get; set; } = null!;

    [Required]
    [Column("firstname")]
    [StringLength(100)]
    public string Firstname { get; set; } = null!;

    [Required]
    [Column("email")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    [Column("username")]
    [StringLength(100)]
    public string Username { get; set; } = null!;

    [Required]
    [Column("password")]
    [StringLength(255)]
    public string Password { get; set; } = null!;

    [Column("managed_by")]
    [StringLength(50)]
    public string? ManagedBy { get; set; }

    [Column("date_created")]
    public DateTime DateCreated { get; set; }

    // Navigation property
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}