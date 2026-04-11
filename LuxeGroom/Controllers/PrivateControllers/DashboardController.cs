/*
 * DashboardController.cs
 * Handles the admin/staff Dashboard view for LuxeGroom.
 *   (sum of AmountDue where Status = "Paid" for current month).
 */

using LuxeGroom.Data;
using LuxeGroom.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class DashboardController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public DashboardController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Expose session data to layout for nav visibility
            ViewBag.Username = username;
            ViewBag.Role = role;

            var today = DateTime.Today;
            var month = today.Month;
            var year = today.Year;

            // Count approved reservations today
            var appointmentsToday = _context.Reservations
                .Count(r =>
                    r.ReservationDate.Date == today &&
                    r.Status == "Approved");

            // Total registered customers
            var totalClients = _context.Customers.Count();

            // Real revenue — sum of AmountDue from Payments with Status = "Paid" this month
            var revenueThisMonth = _context.Payments
                .Where(p =>
                    p.Status == "Paid" &&
                    p.PaidAt.HasValue &&
                    p.PaidAt.Value.Month == month &&
                    p.PaidAt.Value.Year == year)
                .Sum(p => (decimal?)p.AmountDue) ?? 0m;

            // 3 most recent reservations
            var recent = _context.Reservations
                .OrderByDescending(r => r.ReservationDate)
                .Take(3)
                .Select(r => new RecentAppointmentItem
                {
                    OwnerName = r.OwnerName,
                    PetName = r.PetName,
                    GroomingStyle = r.GroomingStyle,
                    ReservationDate = r.ReservationDate,
                    Status = r.Status
                })
                .ToList();

            var vm = new DashboardViewModel
            {
                Username = username!,
                Role = role ?? "",
                AppointmentsToday = appointmentsToday,
                TotalClients = totalClients,
                RevenueThisMonth = revenueThisMonth,
                RecentAppointments = recent
            };

            return View("/Views/Private/Dashboard.cshtml", vm);
        }
    }
}