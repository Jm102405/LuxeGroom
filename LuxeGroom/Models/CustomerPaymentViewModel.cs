/*
 * CustomerPaymentViewModel.cs
 * ViewModel for CustomerPortal Payment page.
 */

namespace LuxeGroom.Models
{
    public class CustomerPaymentViewModel
    {
        public int PaymentId { get; set; }
        public string ReservationId { get; set; } = null!;
        public string? PetName { get; set; }
        public string? PetSize { get; set; }
        public string? GroomingStyle { get; set; }
        public DateTime ReservationDate { get; set; }
        public decimal AmountDue { get; set; }
        public string Status { get; set; } = "Unpaid";
        public string? ReferenceNumber { get; set; }
        public string? ReceiptImage { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}