/*
 * LoginController.cs
 * Handles Login and Logout for LuxeGroom.
 */

using LuxeGroom.Data;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.Auth
{
    public class LoginController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        // Inject the database context
        public LoginController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // GET — render the Login form
        public IActionResult Index()
        {
            return View("~/Views/Auth/Login.cshtml");
        }

        // POST — validate credentials and create session
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(LoginViewModel model)
        {
            // Return error if username field is empty
            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ViewBag.ErrorMessage = "Please fill in the required field.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            // Return error if password field is empty
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.ErrorMessage = "Please fill in the required field.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            // Look up user by username
            var user = _context.Users
                .FirstOrDefault(u => u.Username == model.Username);

            // Return error if no matching user found
            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            // Block login if the account is deactivated
            if (user.Status == "Inactive")
            {
                ViewBag.ErrorMessage = "Your account has been deactivated. Please contact your administrator.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            // Verify submitted password against the stored BCrypt hash
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (isPasswordValid)
            {
                // Store username and role in session on successful login
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                return RedirectToAction("Dashboard", "Dashboard");
            }

            // Return error if password does not match
            ViewBag.ErrorMessage = "Invalid username or password.";
            return View("~/Views/Auth/Login.cshtml", model);
        }

        // Clear session and redirect to login page on logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}
