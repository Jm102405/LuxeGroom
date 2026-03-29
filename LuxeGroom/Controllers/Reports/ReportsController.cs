/*
 * ReportsController.cs
 * Handles all Report views for LuxeGroom admin.
 * Updated in Thread3.2: Added stub actions for Revenue, GroomingStyle, Payment, MonthlySummary reports.
 */

using LuxeGroom.Data;
using LuxeGroom.Data.Generated;
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
         * Blocks access if the user is not logged in or not an Admin.
         * - No session → redirect to Login page
         * - Logged in but not Admin (e.g., Staff) → redirect to Dashboard
         * - Admin → returns null (proceed normally)
         */
        private IActionResult? GuardAdmin()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            // No active session — redirect to login
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Logged in but insufficient role — redirect to dashboard silently
            if (role != "Admin")
                return RedirectToAction("Dashboard", "Dashboard");

            // Access granted
            return null;
        }

        /*
         * SetViewBag()
         * Populates ViewBag with session values needed by _Layout.cshtml.
         * Called in every action so the sidebar renders correctly (role-based nav).
         */
        private void SetViewBag()
        {
            ViewBag.Role = HttpContext.Session.GetString("Role");
            ViewBag.Username = HttpContext.Session.GetString("Username");
        }

        /*
         * ReservationReport — GET: /Reports/ReservationReport
         * Displays a filterable list of all reservations.
         * Filters: Date From, Date To, Status (Pending / Approved / Cancelled)
         * Results are ordered by most recent reservation date.
         */
        public async Task<IActionResult> ReservationReport(string dateFrom, string dateTo, string status)
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            var query = _context.Reservations.AsQueryable();

            // Apply date range filters if provided
            if (DateTime.TryParse(dateFrom, out var from))
                query = query.Where(r => r.ReservationDate >= from);

            if (DateTime.TryParse(dateTo, out var to))
                query = query.Where(r => r.ReservationDate <= to);

            // Apply status filter if selected
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var reservations = await query
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            // Pass results and filter values back to the view
            ViewBag.Reservations = reservations;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.Status = status;

            return View();
        }

        /*
         * CustomerReport — GET: /Reports/CustomerReport
         * Displays a searchable list of all registered customers.
         * Filters: Search by Name, Search by Email
         * Results are ordered alphabetically by first name.
         */
        public async Task<IActionResult> CustomerReport(string searchName, string searchEmail)
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            var query = _context.Customers.AsQueryable();

            // Apply name filter if provided
            if (!string.IsNullOrEmpty(searchName))
                query = query.Where(c => c.Firstname.Contains(searchName));

            // Apply email filter if provided
            if (!string.IsNullOrEmpty(searchEmail))
                query = query.Where(c => c.Email.Contains(searchEmail));

            var customers = await query
                .OrderBy(c => c.Firstname)
                .ToListAsync();

            // Pass results and filter values back to the view
            ViewBag.Customers = customers;
            ViewBag.SearchName = searchName;
            ViewBag.SearchEmail = searchEmail;

            return View();
        }

        /*
         * RevenueReport — GET: /Reports/RevenueReport
         * Shows estimated revenue based on approved reservations.
         * UI stub — functional logic to be implemented in a future thread.
         */
        public IActionResult RevenueReport()
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            return View();
        }

        /*
         * GroomingStyleReport — GET: /Reports/GroomingStyleReport
         * Shows breakdown of reservations by grooming style (most to least popular).
         * UI stub — functional logic to be implemented in a future thread.
         */
        public IActionResult GroomingStyleReport()
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            return View();
        }

        /*
         * PaymentReport — GET: /Reports/PaymentReport
         * Shows payment records including reference numbers and receipt status.
         * UI stub — functional logic to be implemented in a future thread.
         */
        public IActionResult PaymentReport()
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            return View();
        }

        /*
         * MonthlySummaryReport — GET: /Reports/MonthlySummaryReport
         * Shows a month-by-month summary of new customers and approved bookings.
         * UI stub — functional logic to be implemented in a future thread.
         */
        public IActionResult MonthlySummaryReport()
        {
            var guard = GuardAdmin();
            if (guard != null) return guard;
            SetViewBag();

            return View();
        }
    }
}