/*
 * VerifyCodeController.cs
 * Step 4 of the Forgot Password flow for LuxeGroom.
 * Validates the submitted OTP against the DB then redirects to ResetPassword.
 */

using LuxeGroom.Data;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.Auth
{
    public class VerifyCodeController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        // Inject the database context
        public VerifyCodeController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // GET — render the VerifyCode form with masked Gmail
        public IActionResult Index()
        {
            // Retrieve the username passed from Step 3
            string username = TempData["Username"]?.ToString();

            // Redirect to Step 1 if username is missing
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "ForgotPassword");

            // Keep TempData alive for the next request
            TempData.Keep("Username");
            TempData.Keep("MaskedGmail");

            // Pass username and masked Gmail to the view
            ViewBag.Username = username;
            ViewBag.MaskedGmail = TempData["MaskedGmail"]?.ToString();

            // Render the VerifyCode view
            return View("~/Views/Auth/VerifyCode.cshtml");
        }

        // POST — validate the submitted OTP and proceed to Step 5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string Username, string Code)
        {
            // Return error if code is empty or not exactly 6 digits
            if (string.IsNullOrWhiteSpace(Code) || Code.Length != 6)
            {
                ViewBag.ErrorMessage = "Please enter the complete 6-digit code.";
                ViewBag.Username = Username;
                return View("~/Views/Auth/VerifyCode.cshtml");
            }

            // Look up the user by username
            var user = _context.Users
                .FirstOrDefault(u => u.Username == Username);

            // Return error if user not found
            if (user == null)
            {
                ViewBag.ErrorMessage = "User not found.";
                ViewBag.Username = Username;
                return View("~/Views/Auth/VerifyCode.cshtml");
            }

            // Mask Gmail for display on error returns
            string maskedGmail = user.Gmail.Length > 3
                ? user.Gmail.Substring(0, 3) + "***" + user.Gmail.Substring(user.Gmail.IndexOf('@'))
                : "***" + user.Gmail.Substring(user.Gmail.IndexOf('@'));

            // Return error if OTP has expired
            if (user.ResetCodeExpiry == null || DateTime.Now > user.ResetCodeExpiry)
            {
                ViewBag.ErrorMessage = "Your code has expired. Please request a new one.";
                ViewBag.Username = Username;
                ViewBag.MaskedGmail = maskedGmail;
                return View("~/Views/Auth/VerifyCode.cshtml");
            }

            // Return error if submitted code does not match the stored OTP
            if (user.ResetCode != Code)
            {
                ViewBag.ErrorMessage = "Invalid code. Please try again.";
                ViewBag.Username = Username;
                ViewBag.MaskedGmail = maskedGmail;
                return View("~/Views/Auth/VerifyCode.cshtml");
            }

            // Mark user as verified and redirect to Step 5 — ResetPassword
            TempData["VerifiedUser"] = Username;
            return RedirectToAction("Index", "ResetPassword");
        }
    }
}
