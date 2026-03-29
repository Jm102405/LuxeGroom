/*
 * LoginViewModel.cs
 * Model used by the Login page in LuxeGroom.
 * Holds the username and password submitted from the login form.
 */

namespace LuxeGroom.Models
{
    public class LoginViewModel
    {
        public string Username { get; set; }   // Username entered on the login form
        public string Password { get; set; }   // Password entered on the login form
    }
}
