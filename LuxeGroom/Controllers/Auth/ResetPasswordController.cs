/*
 * ResetPasswordController.cs
 * Step 5 of the Forgot Password flow for LuxeGroom.
 * Hashes and saves the new password, clears OTP fields.
 */

using LuxeGroom.Data;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.Auth
{
    public class ResetPasswordController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        // Inject the database context
        public ResetPasswordController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // GET — render the ResetPassword form for the verified user
        public IActionResult Index()
        {
            // Retrieve the verified username from TempData
            string username = TempData["VerifiedUser"]?.ToString();

            // Redirect to Step 1 if no verified user is found
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "ForgotPassword");

            // Keep TempData alive for the next request
            TempData.Keep("VerifiedUser");

            // Pass username to the view
            ViewBag.Username = username;
            return View("~/Views/Auth/ResetPassword.cshtml");
        }

        // POST — validate, hash, and save the new password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string Username, string Password, string ConfirmPassword)
        {
            // Retrieve verified username from TempData
            string verifiedUser = TempData["VerifiedUser"]?.ToString();

            // Redirect to Step 1 if TempData is missing or username was tampered
            if (string.IsNullOrEmpty(verifiedUser) || verifiedUser != Username)
                return RedirectToAction("Index", "ForgotPassword");

            // Return error if passwords are empty or do not match
            if (string.IsNullOrWhiteSpace(Password) || Password != ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match or are empty.";
                ViewBag.Username = Username;
                TempData.Keep("VerifiedUser");
                return View("~/Views/Auth/ResetPassword.cshtml");
            }

            // Look up the user by username
            var user = _context.Users
                .FirstOrDefault(u => u.Username == Username);

            // Redirect to Step 1 if user no longer exists
            if (user == null)
                return RedirectToAction("Index", "ForgotPassword");

            // Hash and save the new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);

            // Clear the OTP code and expiry after successful reset
            user.ResetCode = null;
            user.ResetCodeExpiry = null;
            _context.SaveChanges();

            // Set success message and redirect to login
            TempData["SuccessMessage"] = "Password reset successfully. Please login with your new password.";
            return RedirectToAction("Index", "Login");
        }
    }
}
