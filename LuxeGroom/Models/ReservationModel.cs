/*
 * ReservationViewModel.cs
 * Binds the public reservation form submission.
 */

namespace LuxeGroom.Models
{
    public class ReservationViewModel
    {
        // Name of the pet owner submitted from the form
        public string OwnerName { get; set; } = string.Empty;

        // Name of the pet being groomed
        public string PetName { get; set; } = string.Empty;

        // Selected grooming style or package
        public string GroomingStyle { get; set; } = string.Empty;

        // Owner's contact phone number
        public string PhoneNumber { get; set; } = string.Empty;

        // Owner's email address
        public string Email { get; set; } = string.Empty;

        // Requested date and time for the reservation
        public DateTime ReservationDate { get; set; }
    }
}
