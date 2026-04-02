/*
 * LoginController.cs
 * Handles Login and Logout for LuxeGroom.
 * Updated in Thread 3.7: Added role-based redirect after login.
 */

using LuxeGroom.Data;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.Auth
{
    public class LoginController : Controller
    {
        private readonly LuxeGroomDbContext _context;

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
            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ViewBag.ErrorMessage = "Please fill in the required field.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.ErrorMessage = "Please fill in the required field.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            var user = _context.Users
                .FirstOrDefault(u => u.Username == model.Username);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            if (user.Status == "Inactive")
            {
                ViewBag.ErrorMessage = "Your account has been deactivated. Please contact your administrator.";
                return View("~/Views/Auth/Login.cshtml", model);
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (isPasswordValid)
            {
                // Store session data
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);

                // Role-based redirect
                if (user.Role == "Customer")
                    return RedirectToAction("Index", "CustomerPortal");

                // Admin and User (Staff) go to Dashboard
                return RedirectToAction("Dashboard", "Dashboard");
            }

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