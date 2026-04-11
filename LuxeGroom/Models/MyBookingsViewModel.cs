/*
 * CustomerMyBookingsViewModel.cs
 * ViewModel for the Customer Portal My Bookings page.
 */

namespace LuxeGroom.Models
{
    public class CustomerMyBookingsViewModel
    {
        public List<BookingItemViewModel> Bookings { get; set; } = new();
    }

    public class BookingItemViewModel
    {
        public string ReservationId { get; set; } = "";
        public string PetName { get; set; } = "";
        public string PetSize { get; set; } = "";
        public string GroomingStyle { get; set; } = "";
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; } = "";

        // Payment info — optional, not used in simplified view
        public string? PaymentStatus { get; set; }
        public decimal? AmountDue { get; set; }
    }
}