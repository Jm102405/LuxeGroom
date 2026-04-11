/*
 * MyBookingsController.cs
 * Handles the Customer Portal My Bookings page for LuxeGroom.
 * GET: /MyBookings — shows all reservations where owner_name matches logged-in username
 */

using LuxeGroom.Data;
using LuxeGroom.Models;
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

            // 1) Get logged-in username from session
            string? username = HttpContext.Session.GetString("Username");

            // TEMP DEBUG — remove after confirming bookings show
            ViewBag.DebugUsername = username;

            if (string.IsNullOrEmpty(username))
            {
                var empty = new CustomerMyBookingsViewModel { Bookings = new() };
                return View("~/Views/Private/CustomerPortal/MyBookings.cshtml", empty);
            }

            // 2) Fetch reservations — case-insensitive match on owner_name
            var reservations = _context.Reservations
                .AsEnumerable()
                .Where(r => !string.IsNullOrEmpty(r.OwnerName) &&
                            r.OwnerName.Trim().Equals(username.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.ReservationDate)
                .ToList();

            // 3) Map to ViewModel
            var bookings = reservations.Select(r => new BookingItemViewModel
            {
                ReservationId = r.Id,
                PetName = r.PetName,
                PetSize = r.PetSize,
                GroomingStyle = r.GroomingStyle,
                ReservationDate = r.ReservationDate,
                Status = r.Status,
                PaymentStatus = null,
                AmountDue = null
            }).ToList();

            var vm = new CustomerMyBookingsViewModel { Bookings = bookings };

            return View("~/Views/Private/CustomerPortal/MyBookings.cshtml", vm);
        }
    }
}