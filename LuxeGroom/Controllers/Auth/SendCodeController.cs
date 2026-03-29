/*
 * SendCodeController.cs
 * Step 3 of the Forgot Password flow for LuxeGroom.
 * Generates OTP, saves to DB, sends via Gmail SMTP, redirects to VerifyCode.
 */

using LuxeGroom.Data;
using LuxeGroom.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace LuxeGroom.Controllers.Auth
{
    public class SendCodeController : Controller
    {
        private readonly LuxeGroomDbContext _context;
        private readonly EmailService _emailService;

        // Inject the database context and email service
        public SendCodeController(LuxeGroomDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET — generate OTP, save to DB, send email, redirect to Step 4
        public IActionResult Index(string username, string method)
        {
            // Redirect to Step 1 if username is missing
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "ForgotPassword");

            // Look up the user by username
            var user = _context.Users
                .FirstOrDefault(u => u.Username == username);

            // Redirect to Step 1 if user not found
            if (user == null)
                return RedirectToAction("Index", "ForgotPassword");

            // Generate a cryptographically secure 6-digit OTP
            string otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Set OTP expiry to 15 minutes from now
            DateTime expiry = DateTime.Now.AddMinutes(15);

            // Save the OTP and expiry to the user record
            user.ResetCode = otp;
            user.ResetCodeExpiry = expiry;
            _context.SaveChanges();

            // Send the OTP to the user's Gmail via SMTP
            _emailService.SendResetCodeEmail(user.Gmail, username, otp);

            // Mask the Gmail — show first 3 chars then *** before the domain
            string maskedGmail = user.Gmail.Length > 3
                ? user.Gmail.Substring(0, 3) + "***" + user.Gmail.Substring(user.Gmail.IndexOf('@'))
                : "***" + user.Gmail.Substring(user.Gmail.IndexOf('@'));

            // Pass username and masked Gmail to Step 4 via TempData
            TempData["Username"] = username;
            TempData["MaskedGmail"] = maskedGmail;

            // Redirect to Step 4 — VerifyCode
            return RedirectToAction("Index", "VerifyCode");
        }
    }
}
