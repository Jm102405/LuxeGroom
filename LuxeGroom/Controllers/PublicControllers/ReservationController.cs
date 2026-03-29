/*
 * ReservationController.cs
 * Handles the public reservation form submission from the landing page.
 * Saves new reservation with Status = "Pending".
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

        // Inject the database context
        public ReservationController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // POST — validate form, build reservation entity, and save to DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ReservationViewModel model)
        {
            // Return error if any required field is empty
            if (string.IsNullOrWhiteSpace(model.OwnerName) ||
                string.IsNullOrWhiteSpace(model.PetName) ||
                string.IsNullOrWhiteSpace(model.GroomingStyle) ||
                string.IsNullOrWhiteSpace(model.PhoneNumber) ||
                string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["ReservationError"] = "All fields are required.";
                return Redirect("/#reservation");
            }

            // Generate a sequential RES-prefixed ID based on current count
            int count = _context.Reservations.Count();
            string newId = $"RES-{count + 1}";

            // Build the new reservation entity with Pending status
            var reservation = new Reservation
            {
                Id = newId,
                OwnerName = model.OwnerName,
                PetName = model.PetName,
                GroomingStyle = model.GroomingStyle,
                Phone = model.PhoneNumber,
                Email = model.Email,
                ReservationDate = model.ReservationDate,
                Status = "Pending",
                CustomerId = null
            };

            // Save the reservation to the database
            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            // Set success message and redirect back to the reservation section
            TempData["ReservationSuccess"] = "Your reservation has been submitted!";
            return Redirect("/#reservation");
        }
    }
}
