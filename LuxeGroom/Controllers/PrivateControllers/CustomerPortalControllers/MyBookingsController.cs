    /*
     * MyBookingsController.cs
     * Handles the Customer Portal My Bookings page for LuxeGroom.
     * Extracted in Thread 3.9 from CustomerPortalController.cs.
     * GET: /MyBookings — shows customer's reservation history
     */

    using LuxeGroom.Data;
    using Microsoft.AspNetCore.Mvc;

    namespace LuxeGroom.Controllers.PrivateControllers.CustomerPortalControllers
    {
        public class MyBookingsController : Controller
        {
            private readonly LuxeGroomDbContext _context;

            public MyBookingsController(LuxeGroomDbContext context)
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

            // ─── GET: /MyBookings ──────────────────────────────────────────

            public IActionResult Index()
            {
                if (!IsCustomerLoggedIn())
                    return RedirectToAction("Index", "Login");

                SetViewBagSession();
                ViewBag.Title = "My Bookings";
                return View("~/Views/Private/CustomerPortal/MyBookings.cshtml");
            }
        }
    }