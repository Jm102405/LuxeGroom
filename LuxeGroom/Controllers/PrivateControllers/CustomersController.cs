/*
 * CustomersController.cs
 * Lists customers with their latest reservation info for the Customers page.
 */

using LuxeGroom.Data;           // DbContext + entities
using LuxeGroom.Models;         // CustomerListItemViewModel
using Microsoft.AspNetCore.Mvc; // MVC Controller base

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class CustomersController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public CustomersController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // Simple session guard for private pages
        private IActionResult? GuardSession()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Login", "Account");

            return null;
        }

        // GET: /Customers/Customers
        public IActionResult Customers()
        {
            // Enforce login
            var guard = GuardSession();
            if (guard != null) return guard;

            // Expose session data to layout
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");

            // For each customer, get latest Approved reservation by email
            var query =
                from c in _context.Customers
                join r in _context.Reservations
                    on c.Email equals r.Email into cr
                from r in cr
                    .Where(x => x.Status == "Approved")
                    .OrderByDescending(x => x.ReservationDate)
                    .Take(1)
                    .DefaultIfEmpty()
                select new CustomerListItemViewModel
                {
                    CustomerId = c.CustomerId,
                    FullName = c.Firstname,
                    Email = c.Email,
                    Phone = c.Phone,
                    ManagedBy = c.ManagedBy ?? "-",
                    PetName = r != null ? r.PetName : null,
                    GroomingStyle = r != null ? r.GroomingStyle : null,
                    ReservationDate = r != null ? r.ReservationDate : null
                };

            var model = query.ToList();

            // Render the Customers view with data
            return View("/Views/Private/Customers.cshtml", model);
        }
    }
}
