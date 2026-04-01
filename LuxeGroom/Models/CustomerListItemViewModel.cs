/*
 * CustomerListItemViewModel.cs
 * ViewModel for Customers.cshtml list.
 * Updated in Thread 3.7: Added payment info (status, amount, reference, receipt, managed by).
 */

namespace LuxeGroom.Models
{
    public class CustomerListItemViewModel
    {
        // Basic customer info
        public string CustomerId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string ManagedBy { get; set; } = null!;

        // Latest approved reservation details
        public string? PetName { get; set; }
        public string? PetSize { get; set; }
        public string? GroomingStyle { get; set; }
        public DateTime? ReservationDate { get; set; }
        public string? ReservationId { get; set; }

        // Payment info — NEW (Thread 3.7)
        public int? PaymentId { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public decimal? AmountDue { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ReceiptImage { get; set; }
        public string? PaymentManagedBy { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}