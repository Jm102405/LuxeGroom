/*
 * PaymentController.cs
 * Handles the Customer Portal Payment page for LuxeGroom.
 * Extracted in Thread 4.0 from CustomerPortalController.cs.
 * GET:  /Payment              — loads unpaid/pending payment data for the logged-in customer
 * POST: /Payment/UploadPayment — customer submits GCash reference number + receipt image
 */

using LuxeGroom.Data;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PrivateControllers.CustomerPortalControllers
{
    public class PaymentController : Controller
    {
        private readonly LuxeGroomDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PaymentController(LuxeGroomDbContext context, IWebHostEnvironment env)
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

        // ─── GET: /Payment ─────────────────────────────────────────────

        public IActionResult Index()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "Payment";

            var username = HttpContext.Session.GetString("Username");

            var customer = _context.Customers
                .FirstOrDefault(c => c.Username == username);

            if (customer == null)
                return View("~/Views/Private/CustomerPortal/Payment.cshtml");

            var reservation = _context.Reservations
                .Where(r => r.Email == customer.Email && r.Status == "Approved")
                .OrderByDescending(r => r.ReservationDate)
                .FirstOrDefault();

            if (reservation == null)
                return View("~/Views/Private/CustomerPortal/Payment.cshtml");

            var payment = _context.Payments
                .FirstOrDefault(p => p.ReservationId == reservation.Id);

            if (payment == null)
                return View("~/Views/Private/CustomerPortal/Payment.cshtml");

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

        // ─── POST: /Payment/UploadPayment ──────────────────────────────

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
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                TempData["Error"] = "Please enter your GCash reference number.";
                return RedirectToAction("Index");
            }

            if (receiptImage == null || receiptImage.Length == 0)
            {
                TempData["Error"] = "Please upload your receipt image.";
                return RedirectToAction("Index");
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(receiptImage.ContentType))
            {
                TempData["Error"] = "Only JPG, PNG, or WEBP images are allowed.";
                return RedirectToAction("Index");
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(receiptImage.FileName);
            var fileName = $"{payment.ReservationId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await receiptImage.CopyToAsync(stream);
            }

            payment.ReferenceNumber = referenceNumber.Trim();
            payment.ReceiptImage = $"uploads/receipts/{fileName}";
            payment.Status = "Pending Review";

            _context.SaveChanges();

            TempData["Success"] = "Receipt submitted! Please wait for staff confirmation.";
            return RedirectToAction("Index");
        }
    }
}