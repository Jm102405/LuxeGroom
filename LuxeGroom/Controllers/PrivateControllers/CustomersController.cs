/*
 * CustomersController.cs
 * Lists customers with latest reservation + payment info.
 */

using LuxeGroom.Data;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class CustomersController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public CustomersController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        private IActionResult? GuardSession()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
                return RedirectToAction("Index", "Login");
            return null;
        }

        // GET: /Customers/Customers
        public IActionResult Customers()
        {
            var guard = GuardSession();
            if (guard != null) return guard;

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");

            // For each customer — get latest Approved reservation + its payment
            var customers = _context.Customers.ToList();
            var reservations = _context.Reservations
                .Where(r => r.Status == "Approved")
                .ToList();
            var payments = _context.Payments.ToList();

            var model = customers.Select(c =>
            {
                // Latest approved reservation for this customer by email
                var latestRes = reservations
                    .Where(r => r.Email == c.Email)
                    .OrderByDescending(r => r.ReservationDate)
                    .FirstOrDefault();

                // Payment linked to that reservation
                var payment = latestRes != null
                    ? payments.FirstOrDefault(p => p.ReservationId == latestRes.Id)
                    : null;

                return new CustomerListItemViewModel
                {
                    CustomerId = c.CustomerId,
                    FullName = c.Firstname,
                    Email = c.Email,
                    Phone = c.Phone ?? "-",
                    ManagedBy = c.ManagedBy ?? "-",
                    PetName = latestRes?.PetName,
                    PetSize = latestRes?.PetSize,
                    GroomingStyle = latestRes?.GroomingStyle,
                    ReservationDate = latestRes?.ReservationDate,
                    ReservationId = latestRes?.Id,
                    PaymentId = payment?.Id,
                    PaymentStatus = payment?.Status ?? "Unpaid",
                    AmountDue = payment?.AmountDue,
                    ReferenceNumber = payment?.ReferenceNumber,
                    ReceiptImage = payment?.ReceiptImage,
                    PaymentManagedBy = payment?.ManagedBy,
                    PaidAt = payment?.PaidAt
                };
            }).ToList();

            return View("/Views/Private/Customers.cshtml", model);
        }

        // POST: /Customers/ApprovePayment
        // Staff/Admin approves a customer's uploaded payment receipt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApprovePayment(int paymentId)
        {
            var guard = GuardSession();
            if (guard != null) return guard;

            var payment = _context.Payments.FirstOrDefault(p => p.Id == paymentId);
            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction("Customers");
            }

            var approvedBy = HttpContext.Session.GetString("Username") ?? "Unknown";

            payment.Status = "Paid";
            payment.PaidAt = DateTime.Now;
            payment.ManagedBy = approvedBy;

            _context.SaveChanges();

            TempData["Success"] = $"Payment approved by {approvedBy}.";
            return RedirectToAction("Customers");
        }
    }
}