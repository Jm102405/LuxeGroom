/*
 * SettingsController.cs
 * Handles the Customer Portal Settings page for LuxeGroom.
 * GET: /Settings — shows account info (read-only)
 */

using LuxeGroom.Data;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PrivateControllers.CustomerPortalControllers
{
    public class SettingsController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public SettingsController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // ─── Session Guard ─────────────────────────────────────────────

        private bool IsCustomerLoggedIn()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");
            return !string.IsNullOrEmpty(username) && role == "Customer";
        }

        private void SetViewBagSession()
        {
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");
        }

        // ─── GET: /Settings ────────────────────────────────────────────

        public IActionResult Index()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "Settings";

            var username = HttpContext.Session.GetString("Username");

            // Load customer profile from Customers table
            var customer = _context.Customers
                .FirstOrDefault(c => c.Username == username);

            if (customer != null)
            {
                ViewBag.FirstName = customer.Firstname;
                ViewBag.Email = customer.Email;
                ViewBag.Phone = customer.Phone;
            }

            return View("~/Views/Private/CustomerPortal/Settings.cshtml");
        }
    }
}