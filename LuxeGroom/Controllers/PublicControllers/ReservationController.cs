/*
 * ReservationController.cs
 * Handles the public reservation form submission from the landing page.
 * Saves new reservation with Status = "Pending".
 */

using LuxeGroom.Data;
using LuxeGroom.Data.Generated;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LuxeGroom.Controllers.PublicControllers
{
    public class ReservationController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public ReservationController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ReservationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.OwnerName) ||
                string.IsNullOrWhiteSpace(model.PetName) ||
                string.IsNullOrWhiteSpace(model.PetSize) ||
                string.IsNullOrWhiteSpace(model.GroomingStyle) ||
                string.IsNullOrWhiteSpace(model.PhoneNumber) ||
                string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["ReservationError"] = "All fields are required.";
                return Redirect("/#reservation");
            }

            // 1) Validate Philippine mobile number format: 09XXXXXXXXX
            var phoneRegex = new Regex(@"^09\d{9}$");
            if (!phoneRegex.IsMatch(model.PhoneNumber.Trim()))
            {
                TempData["ReservationError"] = "Please enter a valid Philippine mobile number (e.g. 09XXXXXXXXX).";
                return Redirect("/#reservation");
            }

            // 2) Validate reservation date using PH local time (UTC+8) to avoid server timezone offset
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var phToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;

            var minDate = phToday.AddDays(1);
            var maxDate = phToday.AddDays(2);

            if (model.ReservationDate < minDate || model.ReservationDate > maxDate)
            {
                TempData["ReservationError"] = $"Reservation date must be between {minDate:MMMM d, yyyy} and {maxDate:MMMM d, yyyy}.";
                return Redirect("/#reservation");
            }

            // 3) Block email already in Users table (Admin/Staff)
            bool emailInUsers = _context.Users
                .Any(u => u.Gmail.ToLower() == model.Email.ToLower().Trim());

            if (emailInUsers)
            {
                TempData["ReservationError"] = "This email is already in use. Please use a different email.";
                return Redirect("/#reservation");
            }

            // 4) Block email already in Reservations table (existing customer reservation)
            bool emailInReservations = _context.Reservations
                .Any(r => r.Email.ToLower() == model.Email.ToLower().Trim());

            if (emailInReservations)
            {
                TempData["ReservationError"] = "You already have an existing reservation with this email. Please log in to your Customer Portal.";
                return Redirect("/#reservation");
            }

            int count = _context.Reservations.Count();
            string newId = $"RES-{count + 1}";

            var reservation = new Reservation
            {
                Id = newId,
                OwnerName = model.OwnerName,
                PetName = model.PetName,
                PetSize = model.PetSize,
                GroomingStyle = model.GroomingStyle,
                Phone = model.PhoneNumber,
                Email = model.Email,
                ReservationDate = model.ReservationDate,
                Status = "Pending",
                CustomerId = null
            };

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            TempData["ReservationSuccess"] = "Your reservation has been submitted!";
            return Redirect("/#reservation");
        }
    }
}