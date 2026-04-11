/*
 * ReportsController.cs
 * Handles all Report views for LuxeGroom admin.
 * Updated in Thread 3.2: Added stub actions for all report types.
 * Updated in Thread 4.3.2: Removed Reservation, Customer, Payment reports.
 *   Kept and implemented: Revenue, GroomingStyle, MonthlySummary.
 * Updated in Thread 4.4: Moved view models to Models/ReportViewModels.cs.
 * Updated in Thread 4.4 Step 2: Added drill-down detail JSON actions.
 * Updated in Thread 4.4 Step 2 Fix: In-memory joins for EF Core compatibility.
 * Updated in Thread 4.4 Fix 2: GroomingStyleReport counts paid reservations only.
 * Updated in Thread 4.4 Step 3: Added date range filter to all 3 report tabs.
 */

using LuxeGroom.Data;
using LuxeGroom.Models;
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

        private IActionResult? GuardAdmin()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Index", "Login");
            if (role != "Admin") return RedirectToAction("Dashboard", "Dashboard");
            return null;
        }

        private void SetViewBag()
        {
            ViewBag.Role = HttpContext.Session.GetString("Role");
            ViewBag.Username = HttpContext.Session.GetString("Username");
        }

        // ─── Helper: parse "yyyy-MM" string to DateTime? ─────────────
        private static DateTime? ParseMonth(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateTime.TryParseExact(value, "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }

        /*
         * RevenueReport — GET: /Reports/RevenueReport?from=&to=
         * Shows monthly revenue from Payments table (Status = "Paid").
         * Accepts optional from/to month filters (format: yyyy-MM).
         */
        public async Task<IActionResult> RevenueReport(string? from, string? to)
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            var fromDate = ParseMonth(from);
            var toDate = ParseMonth(to);

            var query = _context.Payments
                .Where(p => p.Status == "Paid" && p.PaidAt.HasValue);

            if (fromDate.HasValue)
                query = query.Where(p =>
                    p.PaidAt!.Value.Year > fromDate.Value.Year ||
                    (p.PaidAt!.Value.Year == fromDate.Value.Year &&
                     p.PaidAt!.Value.Month >= fromDate.Value.Month));

            if (toDate.HasValue)
                query = query.Where(p =>
                    p.PaidAt!.Value.Year < toDate.Value.Year ||
                    (p.PaidAt!.Value.Year == toDate.Value.Year &&
                     p.PaidAt!.Value.Month <= toDate.Value.Month));

            var revenueData = await query
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
            ViewBag.FilterFrom = from;
            ViewBag.FilterTo = to;

            return View("Reports");
        }

        /*
         * RevenueDetail — GET: /Reports/RevenueDetail?year=&month=
         */
        [HttpGet]
        public async Task<IActionResult> RevenueDetail(int year, int month)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return Unauthorized();

            var rows = await _context.Payments
                .Where(p => p.Status == "Paid"
                         && p.PaidAt.HasValue
                         && p.PaidAt!.Value.Year == year
                         && p.PaidAt!.Value.Month == month)
                .Include(p => p.Reservation)
                .OrderBy(p => p.PaidAt)
                .Select(p => new
                {
                    ownerName = p.Reservation != null ? p.Reservation.OwnerName : "—",
                    petName = p.Reservation != null ? p.Reservation.PetName : "—",
                    groomingStyle = p.Reservation != null ? p.Reservation.GroomingStyle : "—",
                    amountDue = p.AmountDue,
                    paidAt = p.PaidAt!.Value.ToString("MMM dd, yyyy")
                })
                .ToListAsync();

            return Json(rows);
        }

        /*
         * GroomingStyleReport — GET: /Reports/GroomingStyleReport?from=&to=
         * Ranks grooming styles by PAID reservations.
         * Accepts optional from/to date filters (format: yyyy-MM-dd).
         */
        public async Task<IActionResult> GroomingStyleReport(string? from, string? to)
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            DateTime? fromDate = null, toDate = null;
            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fd)) fromDate = fd;
            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var td)) toDate = td;

            var paymentsQuery = _context.Payments.Where(p => p.Status == "Paid");

            if (fromDate.HasValue)
                paymentsQuery = paymentsQuery.Where(p =>
                    p.PaidAt.HasValue && p.PaidAt!.Value >= fromDate.Value);

            if (toDate.HasValue)
                paymentsQuery = paymentsQuery.Where(p =>
                    p.PaidAt.HasValue && p.PaidAt!.Value < toDate.Value.AddDays(1));

            var paidReservationIds = await paymentsQuery
                .Select(p => p.ReservationId)
                .ToListAsync();

            var styleData = await _context.Reservations
                .Where(r => paidReservationIds.Contains(r.Id))
                .GroupBy(r => r.GroomingStyle)
                .Select(g => new GroomingStyleItem
                {
                    GroomingStyle = g.Key,
                    ReservationCount = g.Count()
                })
                .OrderByDescending(x => x.ReservationCount)
                .ToListAsync();

            int rank = 1;
            foreach (var item in styleData)
                item.Rank = rank++;

            ViewBag.StyleData = styleData;
            ViewBag.ActiveTab = "grooming";
            ViewBag.FilterFrom = from;
            ViewBag.FilterTo = to;

            return View("Reports");
        }

        /*
         * GroomingStyleDetail — GET: /Reports/GroomingStyleDetail?style=&from=&to=
         */
        [HttpGet]
        public async Task<IActionResult> GroomingStyleDetail(string style, string? from, string? to)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return Unauthorized();

            DateTime? fromDate = null, toDate = null;
            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var fd)) fromDate = fd;
            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var td)) toDate = td;

            var reservations = await _context.Reservations
                .Where(r => r.GroomingStyle == style)
                .ToListAsync();

            var paymentsQuery = _context.Payments.Where(p => p.Status == "Paid");

            if (fromDate.HasValue)
                paymentsQuery = paymentsQuery.Where(p =>
                    p.PaidAt.HasValue && p.PaidAt!.Value >= fromDate.Value);

            if (toDate.HasValue)
                paymentsQuery = paymentsQuery.Where(p =>
                    p.PaidAt.HasValue && p.PaidAt!.Value < toDate.Value.AddDays(1));

            var payments = await paymentsQuery.ToListAsync();

            var rows = reservations
                .Join(
                    payments,
                    r => r.Id,
                    p => p.ReservationId,
                    (r, p) => new
                    {
                        ownerName = r.OwnerName,
                        petName = r.PetName,
                        reservationDate = r.ReservationDate.ToString("MMM dd, yyyy"),
                        amountDue = p.AmountDue,
                        paidAt = p.PaidAt.HasValue
                                          ? p.PaidAt.Value.ToString("MMM dd, yyyy")
                                          : "—"
                    }
                )
                .OrderByDescending(x => x.paidAt)
                .ToList();

            return Json(rows);
        }

        /*
         * MonthlySummaryReport — GET: /Reports/MonthlySummaryReport?from=&to=
         * Accepts optional from/to month filters (format: yyyy-MM).
         */
        public async Task<IActionResult> MonthlySummaryReport(string? from, string? to)
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            var fromDate = ParseMonth(from);
            var toDate = ParseMonth(to);

            // Filter reservations by date range
            var resQuery = _context.Reservations.AsQueryable();

            if (fromDate.HasValue)
                resQuery = resQuery.Where(r =>
                    r.ReservationDate.Year > fromDate.Value.Year ||
                    (r.ReservationDate.Year == fromDate.Value.Year &&
                     r.ReservationDate.Month >= fromDate.Value.Month));

            if (toDate.HasValue)
                resQuery = resQuery.Where(r =>
                    r.ReservationDate.Year < toDate.Value.Year ||
                    (r.ReservationDate.Year == toDate.Value.Year &&
                     r.ReservationDate.Month <= toDate.Value.Month));

            var reservationGroups = await resQuery
                .GroupBy(r => new { r.ReservationDate.Year, r.ReservationDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Count(),
                    Approved = g.Count(r => r.Status == "Approved")
                })
                .ToListAsync();

            // Filter payments by date range
            var payQuery = _context.Payments
                .Where(p => p.Status == "Paid" && p.PaidAt.HasValue);

            if (fromDate.HasValue)
                payQuery = payQuery.Where(p =>
                    p.PaidAt!.Value.Year > fromDate.Value.Year ||
                    (p.PaidAt!.Value.Year == fromDate.Value.Year &&
                     p.PaidAt!.Value.Month >= fromDate.Value.Month));

            if (toDate.HasValue)
                payQuery = payQuery.Where(p =>
                    p.PaidAt!.Value.Year < toDate.Value.Year ||
                    (p.PaidAt!.Value.Year == toDate.Value.Year &&
                     p.PaidAt!.Value.Month <= toDate.Value.Month));

            var revenueGroups = await payQuery
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

            // Filter customers by date range
            var custQuery = _context.Customers.AsQueryable();

            if (fromDate.HasValue)
                custQuery = custQuery.Where(c =>
                    c.DateCreated.Year > fromDate.Value.Year ||
                    (c.DateCreated.Year == fromDate.Value.Year &&
                     c.DateCreated.Month >= fromDate.Value.Month));

            if (toDate.HasValue)
                custQuery = custQuery.Where(c =>
                    c.DateCreated.Year < toDate.Value.Year ||
                    (c.DateCreated.Year == toDate.Value.Year &&
                     c.DateCreated.Month <= toDate.Value.Month));

            var customerGroups = await custQuery
                .GroupBy(c => new { c.DateCreated.Year, c.DateCreated.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    NewCustomers = g.Count()
                })
                .ToListAsync();

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
            ViewBag.FilterFrom = from;
            ViewBag.FilterTo = to;

            return View("Reports");
        }

        /*
         * MonthlySummaryDetail — GET: /Reports/MonthlySummaryDetail?year=&month=
         */
        [HttpGet]
        public async Task<IActionResult> MonthlySummaryDetail(int year, int month)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin") return Unauthorized();

            var reservations = await _context.Reservations
                .Where(r => r.ReservationDate.Year == year && r.ReservationDate.Month == month)
                .ToListAsync();

            var payments = await _context.Payments.ToListAsync();

            var rows = reservations
                .GroupJoin(
                    payments,
                    r => r.Id,
                    p => p.ReservationId,
                    (r, pmts) => new { r, pmts }
                )
                .SelectMany(
                    x => x.pmts.DefaultIfEmpty(),
                    (x, p) => new
                    {
                        ownerName = x.r.OwnerName,
                        petName = x.r.PetName,
                        groomingStyle = x.r.GroomingStyle,
                        status = x.r.Status,
                        reservationDate = x.r.ReservationDate.ToString("MMM dd, yyyy"),
                        revenue = p != null && p.Status == "Paid" ? p.AmountDue : 0
                    }
                )
                .OrderBy(x => x.reservationDate)
                .ToList();

            return Json(rows);
        }
    }
}