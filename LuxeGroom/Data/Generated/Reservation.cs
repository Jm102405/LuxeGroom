/*
 * Reservation.cs
 * EF entity for the Reservations table.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuxeGroom.Data.Generated
{
    [Table("Reservations")]
    public class Reservation
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = string.Empty;

        [Column("owner_name")]
        public string OwnerName { get; set; } = string.Empty;

        [Column("pet_name")]
        public string PetName { get; set; } = string.Empty;

        [Column("pet_size")]
        public string PetSize { get; set; } = string.Empty;

        [Column("grooming_style")]
        public string GroomingStyle { get; set; } = string.Empty;

        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("reservation_date", TypeName = "date")]
        public DateTime ReservationDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("customer_id")]
        public string? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
    }
}