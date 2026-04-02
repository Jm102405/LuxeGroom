    /*
     * DashboardController.cs
     * Handles the admin/staff Dashboard view for LuxeGroom.
     * Fixed in Thread3.2: Added ViewBag.Role so _Layout.cshtml renders
     * Admin-only nav items (Users, Reports) correctly on Dashboard load.
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

                var appointmentsToday = _context.Reservations
                    .Count(r =>
                        r.ReservationDate.Date == today &&
                        r.Status == "Approved");

                var totalClients = _context.Customers.Count();

                var reservationsThisMonth = _context.Reservations
                    .Count(r =>
                        r.ReservationDate.Month == month &&
                        r.ReservationDate.Year == year &&
                        r.Status == "Approved");

                decimal estimatedRevenue = reservationsThisMonth * 800m;

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
                    EstimatedRevenueThisMonth = estimatedRevenue,
                    RecentAppointments = recent
                };

                return View("/Views/Private/Dashboard.cshtml", vm);
            }
        }
    }