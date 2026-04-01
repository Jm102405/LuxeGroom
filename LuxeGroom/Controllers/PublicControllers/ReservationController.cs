/*
 * ReservationController.cs
 * Handles the public reservation form submission from the landing page.
 * Saves new reservation with Status = "Pending".
 * Updated in Thread 3.7: Added PetSize to reservation entity.
 */

using LuxeGroom.Data;
using LuxeGroom.Data.Generated;
using LuxeGroom.Models;
using Microsoft.AspNetCore.Mvc;

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