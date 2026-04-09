/*
 * ReportsController.cs
 * Handles all Report views for LuxeGroom admin.
 * Updated in Thread 3.2: Added stub actions for all report types.
 * Updated in Thread 4.3.2: Removed Reservation, Customer, Payment reports.
 *   Kept and implemented: Revenue, GroomingStyle, MonthlySummary.
 */

using LuxeGroom.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuxeGroom.Controllers.Reports
{
    public class ReportsController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public ReportsController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        /*
         * GuardAdmin()
         * Blocks access if not logged in or not Admin.
         */
        private IActionResult? GuardAdmin()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            if (role != "Admin")
                return RedirectToAction("Dashboard", "Dashboard");

            return null;
        }

        /*
         * SetViewBag()
         * Populates ViewBag with session values needed by _Layout.cshtml.
         */
        private void SetViewBag()
        {
            ViewBag.Role = HttpContext.Session.GetString("Role");
            ViewBag.Username = HttpContext.Session.GetString("Username");
        }

        /*
         * RevenueReport — GET: /Reports/RevenueReport
         * Shows monthly revenue from Payments table (Status = "Paid").
         * Groups by month/year, sums AmountDue per group.
         */
        public async Task<IActionResult> RevenueReport()
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            var revenueData = await _context.Payments
                .Where(p => p.Status == "Paid" && p.PaidAt.HasValue)
                .GroupBy(p => new
                {
                    Year = p.PaidAt!.Value.Year,
                    Month = p.PaidAt!.Value.Month
                })
                .Select(g => new RevenueMonthItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(p => p.AmountDue),
                    PaymentCount = g.Count()
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            ViewBag.RevenueData = revenueData;
            ViewBag.GrandTotal = revenueData.Sum(x => x.TotalRevenue);
            ViewBag.ActiveTab = "revenue";

            return View("Reports");
        }

        /*
         * GroomingStyleReport — GET: /Reports/GroomingStyleReport
         * Ranks grooming styles by total number of reservations (most to least).
         */
        public async Task<IActionResult> GroomingStyleReport()
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            var styleData = await _context.Reservations
                .GroupBy(r => r.GroomingStyle)
                .Select(g => new GroomingStyleItem
                {
                    GroomingStyle = g.Key,
                    ReservationCount = g.Count()
                })
                .OrderByDescending(x => x.ReservationCount)
                .ToListAsync();

            // Assign rank after fetch
            int rank = 1;
            foreach (var item in styleData)
                item.Rank = rank++;

            ViewBag.StyleData = styleData;
            ViewBag.ActiveTab = "grooming";

            return View("Reports");
        }

        /*
         * MonthlySummaryReport — GET: /Reports/MonthlySummaryReport
         * Month-by-month summary: new customers, total reservations,
         * approved reservations, and revenue for that month.
         */
        public async Task<IActionResult> MonthlySummaryReport()
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            // Group reservations by month
            var reservationGroups = await _context.Reservations
                .GroupBy(r => new { r.ReservationDate.Year, r.ReservationDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Count(),
                    Approved = g.Count(r => r.Status == "Approved")
                })
                .ToListAsync();

            // Group paid revenue by month
            var revenueGroups = await _context.Payments
                .Where(p => p.Status == "Paid" && p.PaidAt.HasValue)
                .GroupBy(p => new
                {
                    Year = p.PaidAt!.Value.Year,
                    Month = p.PaidAt!.Value.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(p => p.AmountDue)
                })
                .ToListAsync();

            // Group new customers by month
            var customerGroups = await _context.Customers
                .GroupBy(c => new { c.DateCreated.Year, c.DateCreated.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    NewCustomers = g.Count()
                })
                .ToListAsync();

            // Merge all groups by year/month
            var allMonths = reservationGroups
                .Select(r => new { r.Year, r.Month })
                .Union(revenueGroups.Select(r => new { r.Year, r.Month }))
                .Union(customerGroups.Select(c => new { c.Year, c.Month }))
                .Distinct()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToList();

            var summaryData = allMonths.Select(m => new MonthlySummaryItem
            {
                Year = m.Year,
                Month = m.Month,
                NewCustomers = customerGroups
                                    .FirstOrDefault(c => c.Year == m.Year && c.Month == m.Month)
                                    ?.NewCustomers ?? 0,
                TotalReservations = reservationGroups
                                    .FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)
                                    ?.Total ?? 0,
                ApprovedReservations = reservationGroups
                                    .FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)
                                    ?.Approved ?? 0,
                Revenue = revenueGroups
                                    .FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)
                                    ?.Revenue ?? 0
            }).ToList();

            ViewBag.SummaryData = summaryData;
            ViewBag.ActiveTab = "monthly";

            return View("Reports");
        }
    }

    // ─── View Models ────────────────────────────────────────────────────────────

    public class RevenueMonthItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PaymentCount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    public class GroomingStyleItem
    {
        public int Rank { get; set; }
        public string GroomingStyle { get; set; } = string.Empty;
        public int ReservationCount { get; set; }
    }

    public class MonthlySummaryItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewCustomers { get; set; }
        public int TotalReservations { get; set; }
        public int ApprovedReservations { get; set; }
        public decimal Revenue { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }
}