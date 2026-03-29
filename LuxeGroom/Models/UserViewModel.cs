/*
 * UserViewModel.cs
 * Model used by Add User and Edit User forms in LuxeGroom.
 * Password is no longer submitted from the form — auto-generated in the controller.
 * PhoneNumber is collected during account creation.
 */

namespace LuxeGroom.Models
{
    public class UserViewModel
    {
        public string UserID { get; set; } = string.Empty;        // Unique identifier (e.g. ADM-1, USR-2)
        public string Username { get; set; } = string.Empty;      // Display name used to log in
        public string Gmail { get; set; } = string.Empty;         // Gmail address — must end with @gmail.com
        public string PhoneNumber { get; set; } = string.Empty;   // Contact number — optional
        public string Password { get; set; } = string.Empty;      // Internal use only — never from form, auto-generated in controller
        public string Role { get; set; } = string.Empty;          // Access level — either "Admin" or "User"
        public string Status { get; set; } = "Active";            // Account status — "Active" or "Inactive"
    }
}
