/*
 * Payment.cs
 * EF Core entity for the Payments table in LuxeGroom.
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuxeGroom.Data.Generated;

[Table("Payments")]
public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("reservation_id")]
    [StringLength(50)]
    public string ReservationId { get; set; } = null!;

    [Column("amount_due", TypeName = "decimal(10,2)")]
    public decimal AmountDue { get; set; }

    [Column("reference_number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Column("receipt_image")]
    [StringLength(500)]
    public string? ReceiptImage { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "Unpaid";

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    // Staff/Admin who approved the payment — NEW (Thread 3.7b)
    [Column("managed_by")]
    [StringLength(100)]
    public string? ManagedBy { get; set; }

    [ForeignKey("ReservationId")]
    public Reservation? Reservation { get; set; }
}