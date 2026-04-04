/*
 * SettingsController.cs
 * Handles the Customer Portal Settings page for LuxeGroom.
 * Extracted in Thread 3.9 from CustomerPortalController.cs.
 * GET: /Settings — shows account settings
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
            return View("~/Views/Private/CustomerPortal/Settings.cshtml");
        }
    }
}