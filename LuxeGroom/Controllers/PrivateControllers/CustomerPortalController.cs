/*
 * CustomerPortalController.cs
 * Handles all Customer Portal pages for LuxeGroom.
 * Updated in Thread 3.7: Payment action now loads unpaid/pending payment data.
 *                         Added UploadPayment POST action for receipt submission.
 */

using LuxeGroom.Data;
using LuxeGroom.Data.Generated;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class CustomerPortalController : Controller
    {
        private readonly LuxeGroomDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CustomerPortalController(LuxeGroomDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

        // ─── Index ─────────────────────────────────────────────────────

        public IActionResult Index()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            return RedirectToAction("Payment");
        }

        // ─── Payment ────────────────────────────────────────────────────

        // GET: /CustomerPortal/Payment
        // Loads the latest unpaid or pending-review payment for this customer
        public IActionResult Payment()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "Payment";

            var username = HttpContext.Session.GetString("Username");

            // Get customer record by username
            var customer = _context.Customers
                .FirstOrDefault(c => c.Username == username);

            if (customer == null)
                return View("~/Views/Private/CustomerPortal/Payment.cshtml");

            // Get latest approved reservation for this customer
            var reservation = _context.Reservations
                .Where(r => r.Email == customer.Email && r.Status == "Approved")
                .OrderByDescending(r => r.ReservationDate)
                .FirstOrDefault();

            if (reservation == null)
                return View("~/Views/Private/CustomerPortal/Payment.cshtml");

            // Get payment record linked to that reservation
            var payment = _context.Payments
                .FirstOrDefault(p => p.ReservationId == reservation.Id);

            if (payment == null)
                return View("~/Views/Private/CustomerPortal/Payment.cshtml");

            // Build ViewModel for the view
            var model = new CustomerPaymentViewModel
            {
                PaymentId = payment.Id,
                ReservationId = reservation.Id,
                PetName = reservation.PetName,
                PetSize = reservation.PetSize,
                GroomingStyle = reservation.GroomingStyle,
                ReservationDate = reservation.ReservationDate,
                AmountDue = payment.AmountDue,
                Status = payment.Status,
                ReferenceNumber = payment.ReferenceNumber,
                ReceiptImage = payment.ReceiptImage,
                PaidAt = payment.PaidAt
            };

            return View("~/Views/Private/CustomerPortal/Payment.cshtml", model);
        }

        // POST: /CustomerPortal/UploadPayment
        // Customer submits reference number + receipt image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPayment(
            int paymentId,
            string referenceNumber,
            IFormFile receiptImage)
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            var payment = _context.Payments.FirstOrDefault(p => p.Id == paymentId);
            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction("Payment");
            }

            // Validate reference number
            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                TempData["Error"] = "Please enter your GCash reference number.";
                return RedirectToAction("Payment");
            }

            // Validate receipt image
            if (receiptImage == null || receiptImage.Length == 0)
            {
                TempData["Error"] = "Please upload your receipt image.";
                return RedirectToAction("Payment");
            }

            // Only allow image files
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(receiptImage.ContentType))
            {
                TempData["Error"] = "Only JPG, PNG, or WEBP images are allowed.";
                return RedirectToAction("Payment");
            }

            // Save receipt image to wwwroot/uploads/receipts/
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(receiptImage.FileName);
            var fileName = $"{payment.ReservationId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await receiptImage.CopyToAsync(stream);
            }

            // Update payment record
            payment.ReferenceNumber = referenceNumber.Trim();
            payment.ReceiptImage = $"uploads/receipts/{fileName}";
            payment.Status = "Pending Review";

            _context.SaveChanges();

            TempData["Success"] = "Receipt submitted! Please wait for staff confirmation.";
            return RedirectToAction("Payment");
        }

        // ─── My Bookings ────────────────────────────────────────────────

        public IActionResult MyBookings()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "My Bookings";
            return View("~/Views/Private/CustomerPortal/MyBookings.cshtml");
        }

        // ─── Reserve ────────────────────────────────────────────────────

        public IActionResult Reserve()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "Reserve";
            return View("~/Views/Private/CustomerPortal/Reserve.cshtml");
        }

        // ─── Settings ───────────────────────────────────────────────────

        public IActionResult Settings()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "Settings";
            return View("~/Views/Private/CustomerPortal/Settings.cshtml");
        }
    }
}