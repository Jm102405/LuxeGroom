/*
 * CustomerListItemViewModel.cs
 * ViewModel for Customers.cshtml list.
 * Combines customer info, reservation (pet + schedule) and ManagedBy ID.
 */

namespace LuxeGroom.Models
{
    // One row in the Customers list page
    public class CustomerListItemViewModel
    {
        // Basic customer info
        public string CustomerId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string ManagedBy { get; set; } = null!;   // Staff/Admin ID (ADM-1, USR-1, etc.)

        // Latest approved reservation details for this customer
        public string? PetName { get; set; }
        public string? GroomingStyle { get; set; }
        public DateTime? ReservationDate { get; set; }
    }
}
