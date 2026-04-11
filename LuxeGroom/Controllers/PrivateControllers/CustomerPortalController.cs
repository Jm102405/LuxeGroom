/*
 * CustomerPortalController.cs
 * Handles all Customer Portal pages for LuxeGroom.
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

        // ─── Payment GET ────────────────────────────────────────────────

        public IActionResult Payment()
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

        // ─── Upload Payment POST ────────────────────────────────────────

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

            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                TempData["Error"] = "Please enter your GCash reference number.";
                return RedirectToAction("Payment");
            }

            if (receiptImage == null || receiptImage.Length == 0)
            {
                TempData["Error"] = "Please upload your receipt image.";
                return RedirectToAction("Payment");
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(receiptImage.ContentType))
            {
                TempData["Error"] = "Only JPG, PNG, or WEBP images are allowed.";
                return RedirectToAction("Payment");
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
            return RedirectToAction("Payment");
        }

        // ─── My Bookings ────────────────────────────────────────────────

        public IActionResult MyBookings()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            return RedirectToAction("Index", "MyBookings");
        }

        // ─── Reserve GET ────────────────────────────────────────────────

        public IActionResult Reserve()
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            SetViewBagSession();
            ViewBag.Title = "Reserve";
            return View("~/Views/Private/CustomerPortal/Reserve.cshtml");
        }

        // ─── Reserve POST ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Reserve")]
        public IActionResult ReserveSubmit(
            string petName,
            string petSize,
            string groomingStyle,
            DateTime reservationDate)
        {
            if (!IsCustomerLoggedIn())
                return RedirectToAction("Index", "Login");

            var username = HttpContext.Session.GetString("Username");

            var customer = _context.Customers
                .FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Customer record not found.";
                return RedirectToAction("Reserve");
            }

            // Fix: extract max numeric ID to avoid duplicate key on gaps/deletions
            var lastId = _context.Reservations
                .AsEnumerable()
                .Select(r =>
                {
                    var parts = r.Id.Split('-');
                    return parts.Length == 2 && int.TryParse(parts[1], out int n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            string newId = $"RES-{lastId + 1}";

            var reservation = new Reservation
            {
                Id = newId,
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
            return RedirectToAction("Reserve");
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