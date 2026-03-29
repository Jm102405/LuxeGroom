/*
 * UserController.cs
 * Handles all User Management operations for LuxeGroom.
 * Merged from UsersController.cs and UserFormController.cs.
 * Covers listing, add, edit, deactivate, and activate actions.
 */

using LuxeGroom.Data;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class UserController : Controller
    {
        private readonly LuxeGroomDbContext _context;
        private readonly IConfiguration _configuration;

        // Inject the database context and app configuration
        public UserController(LuxeGroomDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ─── USERS LIST ───────────────────────────────────────────────────────────

        // GET — load all users split into Active and Inactive lists
        public IActionResult Users()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Admin")
                return RedirectToAction("Index", "Login");

            ViewBag.Username = username;
            ViewBag.Role = role;

            var allUsers = _context.Users
                .OrderBy(u => u.Username)
                .ToList();

            ViewBag.ActiveUsers = allUsers.Where(u => u.Status == "Active").ToList();
            ViewBag.InactiveUsers = allUsers.Where(u => u.Status != "Active").ToList();

            return View("/Views/Private/User.cshtml");
        }

        // ─── ADD USER ─────────────────────────────────────────────────────────────

        // GET — validate session and render the Add User form
        public IActionResult AddUser()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Admin")
                return RedirectToAction("Index", "Login");

            ViewBag.Username = username;
            ViewBag.Role = role;
            ViewBag.ShowForm = true;

            return View("/Views/Private/User.cshtml");
        }

        // POST — validate, create user, send welcome email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(UserViewModel model)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Admin")
                return RedirectToAction("Index", "Login");

            ViewBag.Username = username;
            ViewBag.Role = role;
            ViewBag.ShowForm = true;

            if (string.IsNullOrWhiteSpace(model.Username) ||
                string.IsNullOrWhiteSpace(model.Gmail) ||
                string.IsNullOrWhiteSpace(model.Role))
            {
                ViewBag.ErrorMessage = "Username, Email, and Role are required.";
                return View("/Views/Private/User.cshtml", model);
            }

            try
            {
                if (_context.Users.Any(u => u.Username == model.Username))
                {
                    ViewBag.ErrorMessage = "Username already exists. Please choose a different one.";
                    return View("/Views/Private/User.cshtml", model);
                }

                if (_context.Users.Any(u => u.Gmail == model.Gmail))
                {
                    ViewBag.ErrorMessage = "Email address is already in use. Please use a different one.";
                    return View("/Views/Private/User.cshtml", model);
                }

                // Generate a collision-safe UserID by finding the highest existing
                // number for this role prefix, then incrementing it
                string prefix = model.Role == "Admin" ? "ADM" : "USR";
                var existingIds = _context.Users
                    .Where(u => u.Role == model.Role)
                    .Select(u => u.UserId)
                    .ToList();

                int maxNum = existingIds
                    .Select(id =>
                    {
                        var parts = id.Split('-');
                        return parts.Length == 2 && int.TryParse(parts[1], out int n) ? n : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                string newUserID = $"{prefix}-{maxNum + 1}";

                string tempPassword = GenerateTemporaryPassword();
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                var newUser = new Data.Generated.User
                {
                    UserId = newUserID,
                    Username = model.Username,
                    PasswordHash = passwordHash,
                    Gmail = model.Gmail,
                    PhoneNumber = model.PhoneNumber ?? "",
                    Role = model.Role,
                    Status = model.Status ?? "Active",
                    DateCreated = DateTime.Now
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                SendWelcomeEmail(model.Gmail, model.Username, tempPassword);

                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                // Surface the inner exception message for easier debugging
                ViewBag.ErrorMessage = "Error: " + (ex.InnerException?.Message ?? ex.Message);
                return View("/Views/Private/User.cshtml", model);
            }
        }

        // ─── EDIT USER ────────────────────────────────────────────────────────────

        // GET — load user by ID and render the Edit User form
        public IActionResult EditUser(string id)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Admin")
                return RedirectToAction("Index", "Login");

            ViewBag.Username = username;
            ViewBag.Role = role;
            ViewBag.ShowForm = true;
            ViewBag.IsEdit = true;

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return RedirectToAction("Users");

            var model = new UserViewModel
            {
                UserID = user.UserId,
                Username = user.Username,
                Gmail = user.Gmail,
                PhoneNumber = user.PhoneNumber ?? "",
                Role = user.Role,
                Status = user.Status
            };

            return View("/Views/Private/User.cshtml", model);
        }

        // POST — validate and save updated user details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(UserViewModel model)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Admin")
                return RedirectToAction("Index", "Login");

            ViewBag.Username = username;
            ViewBag.Role = role;
            ViewBag.ShowForm = true;
            ViewBag.IsEdit = true;

            if (string.IsNullOrWhiteSpace(model.Username) ||
                string.IsNullOrWhiteSpace(model.Gmail) ||
                string.IsNullOrWhiteSpace(model.Role))
            {
                ViewBag.ErrorMessage = "Username, Email, and Role are required.";
                return View("/Views/Private/User.cshtml", model);
            }

            try
            {
                if (_context.Users.Any(u => u.Username == model.Username && u.UserId != model.UserID))
                {
                    ViewBag.ErrorMessage = "Username already exists. Please choose a different one.";
                    return View("/Views/Private/User.cshtml", model);
                }

                if (_context.Users.Any(u => u.Gmail == model.Gmail && u.UserId != model.UserID))
                {
                    ViewBag.ErrorMessage = "Email address is already in use.";
                    return View("/Views/Private/User.cshtml", model);
                }

                var user = _context.Users.FirstOrDefault(u => u.UserId == model.UserID);
                if (user == null) return RedirectToAction("Users");

                user.Username = model.Username;
                user.Gmail = model.Gmail;
                user.PhoneNumber = model.PhoneNumber ?? "";
                user.Role = model.Role;
                // Do NOT overwrite Status — managed by Activate/Deactivate only

                _context.SaveChanges();

                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                // Surface the inner exception message for easier debugging
                ViewBag.ErrorMessage = "Error: " + (ex.InnerException?.Message ?? ex.Message);
                return View("/Views/Private/User.cshtml", model);
            }
        }

        // ─── STATUS ACTIONS ───────────────────────────────────────────────────────

        // GET — soft-delete a user by setting their status to Inactive
        public IActionResult DeleteUser(string id)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Admin")
                return RedirectToAction("Index", "Login");

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                user.Status = "Inactive";
                _context.SaveChanges();
            }

            return RedirectToAction("Users");
        }

        // GET — restore a user by setting their status back to Active
        public IActionResult ActivateUser(string id)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role != "Admin")
                return RedirectToAction("Index", "Login");

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user != null)
            {
                user.Status = "Active";
                _context.SaveChanges();
            }

            return RedirectToAction("Users");
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────────

        // Generate a random 8-character password with upper, lower, digit, and special chars
        private string GenerateTemporaryPassword()
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

            for (int i = 4; i < 8; i++)
                chars.Add(all[rng.Next(all.Length)]);

            return new string(chars.OrderBy(_ => rng.Next()).ToArray());
        }

        // Send a welcome email with the temporary password via Gmail SMTP
        private void SendWelcomeEmail(string toEmail, string username, string tempPassword)
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

Your LuxeGroom account has been created.

Temporary Password: {tempPassword}

Please login and change your password immediately.

— LuxeGroom Team";

            MailMessage mail = new MailMessage
            {
                From = new MailAddress(senderEmail, "LuxeGroom"),
                Subject = "LuxeGroom Account Created",
                Body = body,
                IsBodyHtml = false
            };
            mail.To.Add(toEmail);
            client.Send(mail);
        }

        // ─── EMAIL CHECK API FOR RESERVATIONS ─────────────────────────────────────

        // GET: /User/CheckEmail?email=...
        // Returns JSON: { exists: true/false } based on Users.Gmail
        [HttpGet]
        public IActionResult CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { exists = false });

            var normalized = email.Trim().ToLower();

            var exists = _context.Users
                .AsEnumerable()
                .Any(u => ((u.Gmail ?? string.Empty).Trim().ToLower()) == normalized);

            return Json(new { exists });
        }
    }
}
