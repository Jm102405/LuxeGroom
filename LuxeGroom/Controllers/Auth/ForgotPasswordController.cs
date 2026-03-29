/*
 * ForgotPasswordController.cs
 * Step 1 of the Forgot Password flow for LuxeGroom.
 * Validates the submitted username and redirects to ChooseMethod.
 */

using LuxeGroom.Data;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.Auth
{
    public class ForgotPasswordController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        // Inject the database context
        public ForgotPasswordController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // GET — render the ForgotPassword form
        public IActionResult Index()
        {
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

        // POST — validate username and proceed to Step 2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string Username)
        {
            // Return error if username field is empty
            if (string.IsNullOrWhiteSpace(Username))
            {
                ViewBag.ErrorMessage = "Please enter your username.";
                return View("~/Views/Auth/ForgotPassword.cshtml");
            }

            // Look up active user by username
            var user = _context.Users
                .FirstOrDefault(u => u.Username == Username && u.Status == "Active");

            // Return error if no matching active user found
            if (user == null)
            {
                ViewBag.ErrorMessage = "Username not found or account is inactive.";
                return View("~/Views/Auth/ForgotPassword.cshtml");
            }

            // Retrieve Gmail and phone, defaulting to empty string if null
            string gmail = user.Gmail ?? "";
            string phone = user.PhoneNumber ?? "";

            // Mask Gmail — show first 3 chars then *** before the domain
            string maskedGmail = gmail.Length > 3
                ? gmail.Substring(0, 3) + "***" + gmail.Substring(gmail.IndexOf('@'))
                : "***" + gmail.Substring(gmail.IndexOf('@'));

            // Mask phone — show only the last 4 digits, or fallback if unavailable
            string maskedPhone = string.IsNullOrEmpty(phone) ? "Not available"
                : phone.Length >= 4
                    ? "*** **** " + phone.Substring(phone.Length - 4)
                    : "*** ****";

            // Pass username and masked contacts to Step 2 via TempData
            TempData["Username"] = Username;
            TempData["MaskedGmail"] = maskedGmail;
            TempData["MaskedPhone"] = maskedPhone;

            // Redirect to Step 2 — ChooseMethod
            return RedirectToAction("Index", "ChooseMethod");
        }
    }
}
