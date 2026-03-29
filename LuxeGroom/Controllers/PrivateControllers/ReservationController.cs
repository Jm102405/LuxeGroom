/*
 * ReservationController.cs
 * Handles Admin Reservation Management for LuxeGroom.
 * Lists reservations and processes Handle Request (Accept / Cancel).
 * Updated in Thread 2.7: password hashing, ManagedBy, Gmail uniqueness check,
 * customer welcome email, and staying on Reservations page after Accept.
 * Updated in Thread 2.8: When accepting a reservation, also creates a User account
 * in the Users table using the same pattern as UserController.AddUser.
 * For Customer role, UserId uses CUST-X (CUST-1, CUST-2, ...) instead of USER/USR.
 */

using LuxeGroom.Data;                 // DbContext
using LuxeGroom.Data.Generated;       // Entities (Reservation, Customer, User)
using Microsoft.AspNetCore.Mvc;       // MVC basics
using System.Net;                     // NetworkCredential
using System.Net.Mail;                // SmtpClient, MailMessage
using BCrypt.Net;                     // BCrypt password hashing

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class ReservationController : Controller
    {
        private readonly LuxeGroomDbContext _context;
        private readonly IConfiguration _configuration;

        public ReservationController(LuxeGroomDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Common session guard for private pages
        private IActionResult? GuardSession()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Login", "Account");

            return null;
        }

        // GET: /Reservation/Reservations
        // Lists all reservations for the admin/staff UI
        public IActionResult Reservations()
        {
            var guard = GuardSession();
            if (guard != null) return guard;

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");

            var reservations = _context.Reservations
                .OrderByDescending(r => r.ReservationDate)
                .ToList();

            return View("/Views/Private/Reservations.cshtml", reservations);
        }

        // POST: /Reservation/HandleAccept
        // Accepts a Pending reservation:
        // - Creates a Customer record (CUST-X)
        // - Creates a User record in Users table (UserId = CUST-X, Role = Customer)
        // - Marks reservation as Approved
        // - Sends welcome email with username + temp password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HandleAccept(string id)
        {
            // 1) Enforce login via session
            var guard = GuardSession();
            if (guard != null) return guard;

            // 2) Load reservation being handled
            var reservation = _context.Reservations.FirstOrDefault(r => r.Id == id);
            if (reservation == null)
            {
                TempData["Error"] = "Reservation not found.";
                return RedirectToAction("Reservations");
            }

            // 3) Gmail uniqueness check against Users table (same idea as UserController)
            var normalizedEmail = (reservation.Email ?? string.Empty).Trim().ToLower();

            var emailInUsers = _context.Users
                .AsEnumerable()
                .Any(u => ((u.Gmail ?? string.Empty).Trim().ToLower()) == normalizedEmail);

            if (emailInUsers)
            {
                TempData["Error"] =
                    "This email already exists in the Users list. Please use a different email or check existing account.";
                return RedirectToAction("Reservations");
            }

            // 4) Compute next CUST-X value based on existing customers
            var maxCustNum = _context.Customers
                .AsEnumerable()
                .Where(c => c.CustomerId != null && c.CustomerId.StartsWith("CUST-"))
                .Select(c =>
                {
                    var parts = c.CustomerId!.Split('-');
                    return parts.Length == 2 && int.TryParse(parts[1], out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            var newCustomerId = $"CUST-{maxCustNum + 1}";

            // 5) Determine ManagedBy — prefer UserId, fallback to Username, else "Unknown"
            var managedBy = HttpContext.Session.GetString("UserId")
                          ?? HttpContext.Session.GetString("Username")
                          ?? "Unknown";

            // 6) Generate temporary password and hash using BCrypt
            var plainPassword = GenerateTempPassword();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            // 7) Build base username from owner name
            var baseUsername = (reservation.OwnerName ?? "customer")
                .Replace(" ", "")
                .ToLower();

            if (string.IsNullOrWhiteSpace(baseUsername))
                baseUsername = "customer";

            // Ensure username is unique in Users table (append number if needed)
            var finalUsername = baseUsername;
            int counter = 1;
            while (_context.Users.Any(u => u.Username == finalUsername))
            {
                finalUsername = $"{baseUsername}{counter}";
                counter++;
            }

            // 8) Build Customer entity from reservation data
            var customer = new Customer
            {
                CustomerId = newCustomerId,
                Firstname = reservation.OwnerName,
                Email = reservation.Email,
                Phone = reservation.Phone,
                Username = finalUsername,
                Password = hashedPassword, // store only the hash
                ManagedBy = managedBy      // store staff/admin ID or Username
            };

            // 9) Build User entity for Users table
            // For Customers: UserId must also be CUST-X (same number series as CustomerId).
            // So we reuse newCustomerId as the UserId.
            var user = new User
            {
                UserId = newCustomerId,                 // <── CUST-X instead of USER/USR
                Username = finalUsername,
                PasswordHash = hashedPassword,
                Gmail = reservation.Email,
                PhoneNumber = reservation.Phone ?? "",
                Role = "Customer",
                Status = "Active",
                DateCreated = DateTime.Now,
                ResetCode = null,
                ResetCodeExpiry = null
            };

            // 10) Save new customer + new user and update reservation status
            _context.Customers.Add(customer);
            _context.Users.Add(user);
            reservation.Status = "Approved"; // keep reservation for history
            _context.SaveChanges();

            // 11) Send welcome email to the customer with username + temporary password
            try
            {
                SendCustomerWelcomeEmail(reservation.Email, finalUsername, plainPassword);
            }
            catch
            {
                // TODO: log error if you add logging later
            }

            // 12) Show toast and reload Reservations page
            TempData["Success"] =
                $"Reservation approved. Customer {newCustomerId} has been created and linked to Users table.";
            return RedirectToAction("Reservations");
        }

        // POST: /Reservation/HandleCancel
        // Cancels a reservation (Status = Cancelled, no delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HandleCancel(string id)
        {
            var guard = GuardSession();
            if (guard != null) return guard;

            var reservation = _context.Reservations.FirstOrDefault(r => r.Id == id);
            if (reservation == null)
            {
                TempData["Error"] = "Reservation not found.";
                return RedirectToAction("Reservations");
            }

            reservation.Status = "Cancelled"; // keep record, just mark as cancelled
            _context.SaveChanges();

            TempData["Success"] = "Reservation has been cancelled and kept for records.";
            return RedirectToAction("Reservations");
        }

        // Generates a random 10-character temporary password
        private static string GenerateTempPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%&*";
            const string all = upper + lower + digits + special;

            var rng = new Random();

            var chars = new List<char>
            {
                upper[rng.Next(upper.Length)],
                lower[rng.Next(lower.Length)],
                digits[rng.Next(digits.Length)],
                special[rng.Next(special.Length)]
            };

            for (int i = 4; i < 10; i++)
                chars.Add(all[rng.Next(all.Length)]);

            return new string(chars.OrderBy(_ => rng.Next()).ToArray());
        }

        // Sends a welcome email to the new customer with username + temporary password
        private void SendCustomerWelcomeEmail(string toEmail, string username, string tempPassword)
        {
            string smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "";
            int smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            string senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
            string senderPassword = _configuration["EmailSettings:SenderPassword"] ?? "";

            using SmtpClient client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(senderEmail, senderPassword)
            };

            string body = $@"Hello {username}!

Your LuxeGroom reservation has been approved and your account is now ready.

You can use the following credentials to access your customer portal:

Username: {username}
Temporary Password: {tempPassword}

Please login and change your password immediately after signing in.

— LuxeGroom Team";

            MailMessage mail = new MailMessage
            {
                From = new MailAddress(senderEmail, "LuxeGroom"),
                Subject = "Your LuxeGroom Account Details",
                Body = body,
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);
            client.Send(mail);
        }
    }
}
