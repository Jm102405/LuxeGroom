    /*
 * Customer.cs
 * EF Core entity for the Customers table in LuxeGroom.
 * Stores customer records created when a reservation is accepted.
 * Created in Thread 2.7.
 */

namespace LuxeGroom.Data.Generated;

public partial class Customer
{
    // Primary key — format: CUST-1, CUST-2, etc.
    public string CustomerId { get; set; } = null!;

    // Customer's full name — copied from reservation OwnerName
    public string Firstname { get; set; } = null!;

    // Customer's email address
    public string Email { get; set; } = null!;

    // Customer's phone number
    public string Phone { get; set; } = null!;

    // Auto-generated username derived from the owner's name
    public string Username { get; set; } = null!;

    // Temporary password assigned on customer creation
    public string Password { get; set; } = null!;

    // ID of the staff/admin who accepted the reservation (e.g., ADM-1, USR-1)
    public string? ManagedBy { get; set; }

    // Timestamp of when the customer record was created
    public DateTime CreatedAt { get; set; }
}
