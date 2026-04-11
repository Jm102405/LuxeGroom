/*
 * ReservationController.cs
 * Handles Admin Reservation Management for LuxeGroom.
 */

using LuxeGroom.Data;
using LuxeGroom.Data.Generated;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

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

        private IActionResult? GuardSession()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Index", "Login");
            return null;
        }

        // ── Pricing table (must match frontend JS) ──
        private static readonly Dictionary<string, Dictionary<string, int>> Prices = new()
        {
            ["Bath & Brush"] = new() { ["Small Dog"] = 300, ["Medium Dog"] = 600, ["Large Dog"] = 750 },
            ["Full Groom"] = new() { ["Small Dog"] = 900, ["Medium Dog"] = 1000, ["Large Dog"] = 1250 },
            ["Custom Cut"] = new() { ["Small Dog"] = 1200, ["Medium Dog"] = 1300, ["Large Dog"] = 1500 }
        };

        // GET: /Reservation/Reservations
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HandleAccept(
            string id,
            string petName,
            string petSize,
            string groomingStyle,
            DateTime reservationDate)
        {
            var guard = GuardSession();
            if (guard != null) return guard;

            // 1) Load reservation
            var reservation = _context.Reservations.FirstOrDefault(r => r.Id == id);
            if (reservation == null)
            {
                TempData["Error"] = "Reservation not found.";
                return RedirectToAction("Reservations");
            }

            // 2) Check if customer already has an account
            bool hasExistingAccount = !string.IsNullOrEmpty(reservation.CustomerId);

            // 3) Compute amount + 50% down payment
            int totalAmount = 0;
            if (Prices.TryGetValue(groomingStyle, out var sizeMap) &&
                sizeMap.TryGetValue(petSize, out var price))
            {
                totalAmount = price;
            }
            decimal downPayment = Math.Round(totalAmount * 0.5m, 2);

            // 4) Update reservation with edited fields
            reservation.PetName = petName;
            reservation.PetSize = petSize;
            reservation.GroomingStyle = groomingStyle;
            reservation.ReservationDate = reservationDate;
            reservation.Status = "Approved";

            // 5) Auto-create Payment record — ID is auto-incremented by EF Core
            var payment = new Payment
            {
                ReservationId = reservation.Id,
                AmountDue = downPayment,
                ReferenceNumber = null,
                ReceiptImage = null,
                Status = "Unpaid",
                PaidAt = null
            };
            _context.Payments.Add(payment);

            string successMsg;

            if (hasExistingAccount)
            {
                // ── Existing customer — skip account creation ──
                _context.SaveChanges();

                successMsg = $"Reservation approved. Down payment due: ₱{downPayment:N0}.";
            }
            else
            {
                // ── New customer — create Customer + User account ──

                // 6) Compute next CUST-X
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

                // 7) ManagedBy
                var managedBy = HttpContext.Session.GetString("UserId")
                              ?? HttpContext.Session.GetString("Username")
                              ?? "Unknown";

                // 8) Generate temp password
                var plainPassword = GenerateTempPassword();
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

                // 9) Build unique username
                var baseUsername = (reservation.OwnerName ?? "customer")
                    .Replace(" ", "").ToLower();
                if (string.IsNullOrWhiteSpace(baseUsername)) baseUsername = "customer";

                var finalUsername = baseUsername;
                int counter = 1;
                while (_context.Users.Any(u => u.Username == finalUsername))
                {
                    finalUsername = $"{baseUsername}{counter}";
                    counter++;
                }

                // 10) Create Customer record
                var customer = new Customer
                {
                    CustomerId = newCustomerId,
                    Firstname = reservation.OwnerName,
                    Email = reservation.Email,
                    Phone = reservation.Phone,
                    Username = finalUsername,
                    Password = hashedPassword,
                    ManagedBy = managedBy,
                    DateCreated = DateTime.Now
                };

                // 11) Create User record
                var user = new User
                {
                    UserId = newCustomerId,
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

                _context.Customers.Add(customer);
                _context.Users.Add(user);
                _context.SaveChanges();

                // 12) Link reservation to new customer
                reservation.CustomerId = newCustomerId;
                _context.SaveChanges();

                // 13) Send welcome email
                try
                {
                    SendCustomerWelcomeEmail(
                        reservation.Email,
                        finalUsername,
                        plainPassword,
                        groomingStyle,
                        petSize,
                        totalAmount,
                        (int)downPayment);
                }
                catch { }

                successMsg = $"Reservation approved. Customer {newCustomerId} created. Down payment due: ₱{downPayment:N0}.";
            }

            TempData["Success"] = successMsg;
            return RedirectToAction("Reservations");
        }

        // POST: /Reservation/HandleCancel
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

            reservation.Status = "Cancelled";
            _context.SaveChanges();

            TempData["Success"] = "Reservation has been cancelled and kept for records.";
            return RedirectToAction("Reservations");
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────────

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

        private void SendCustomerWelcomeEmail(
            string toEmail,
            string username,
            string tempPassword,
            string groomingStyle,
            string petSize,
            int totalAmount,
            int downPayment)
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

Reservation Details:
  Grooming Style : {groomingStyle}
  Pet Size       : {petSize}
  Total Amount   : ₱{totalAmount:N0}
  Down Payment   : ₱{downPayment:N0} (50% due before grooming)

Your login credentials:
  Username          : {username}
  Temporary Password: {tempPassword}

Please log in to your Customer Portal and complete your down payment.
Change your password immediately after signing in.

— LuxeGroom Team";

            MailMessage mail = new MailMessage
            {
                From = new MailAddress(senderEmail, "LuxeGroom"),
                Subject = "Your LuxeGroom Reservation is Approved",
                Body = body,
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);
            client.Send(mail);
        }
    }
}