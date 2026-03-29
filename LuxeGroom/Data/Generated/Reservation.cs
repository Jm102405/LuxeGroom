/*
 * Reservation.cs
 * EF entity for the Reservations table.
 * Updated in Thread 3: Added TypeName = "date" to ReservationDate to match SSMS column type.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuxeGroom.Data.Generated
{
    // Maps this class to the Reservations table in the database
    [Table("Reservations")]
    public class Reservation
    {
        // Primary key — RES-prefixed sequential ID
        [Key]
        [Column("id")]
        public string Id { get; set; } = string.Empty;

        // Name of the pet owner
        [Column("owner_name")]
        public string OwnerName { get; set; } = string.Empty;

        // Name of the pet being groomed
        [Column("pet_name")]
        public string PetName { get; set; } = string.Empty;

        // Selected grooming style or package
        [Column("grooming_style")]
        public string GroomingStyle { get; set; } = string.Empty;

        // Owner's contact phone number
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        // Owner's email address
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        // Requested date for the reservation — mapped to SQL date type
        [Column("reservation_date", TypeName = "date")]
        public DateTime ReservationDate { get; set; }

        // Current reservation status — defaults to Pending
        [Column("status")]
        public string Status { get; set; } = "Pending";

        // Optional foreign key linking to a registered customer
        [Column("customer_id")]
        public string? CustomerId { get; set; }
    }
}
