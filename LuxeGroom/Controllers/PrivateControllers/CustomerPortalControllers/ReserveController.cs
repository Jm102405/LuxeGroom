/*
 * ReserveController.cs
 * Handles the Customer Portal Reserve page for LuxeGroom.
 * Extracted in Thread 3.9 from CustomerPortalController.cs.
 * Updated in Thread 3.9: Fixed Customer field names to match Customer.cs entity
 *                         (Firstname only, Phone, CustomerId, DateTime).
 * GET:  /Reserve        — shows reservation form
 * POST: /Reserve/Submit — submits new reservation
 */

using LuxeGroom.Data;
using LuxeGroom.Data.Generated;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PrivateControllers.CustomerPortalControllers
{
    public class ReserveController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public ReserveController(LuxeGroomDbContext context)
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

        // ─── GET: /Reserve ─────────────────────────────────────────────

        public IActionResult Index()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "Reserve";
            return View("~/Views/Private/CustomerPortal/Reserve.cshtml");
        }

        // ─── POST: /Reserve/Submit ─────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(
            string petName,
            string petSize,
            string groomingStyle,
            DateTime reservationDate)
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            var username = HttpContext.Session.GetString("Username");

            // Get customer record by session username
            var customer = _context.Customers
                .FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Customer record not found.";
                return RedirectToAction("Index");
            }

            // Build reservation using Customer entity fields
            var reservation = new Reservation
            {
                OwnerName = customer.Firstname,
                PetName = petName,
                PetSize = petSize,
                GroomingStyle = groomingStyle,
                Phone = customer.Phone,
                Email = customer.Email,
                ReservationDate = reservationDate,
                Status = "Pending",
                CustomerId = customer.CustomerId
            };

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            TempData["Success"] = "true";
            return RedirectToAction("Index");
        }
    }
}