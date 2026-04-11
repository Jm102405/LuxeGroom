/*
 * ReservationViewModel.cs
 * Binds the public reservation form submission.
 */

namespace LuxeGroom.Models
{
    public class ReservationViewModel
    {
        public string OwnerName { get; set; } = string.Empty;

        public string PetName { get; set; } = string.Empty;

        public string PetSize { get; set; } = string.Empty;

        public string GroomingStyle { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime ReservationDate { get; set; }
    }
}