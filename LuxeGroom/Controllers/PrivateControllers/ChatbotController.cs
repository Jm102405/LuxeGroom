/*
 * ChatbotController.cs
 * Handles the Chatbot page at /Chatbot/Chatbot.
 * Responsibilities:
 *   - GET:  Load the most recent Chatbot record from the database and pre-fill the form.
 *   - POST: Update Instructions and Data in the database. If no record exists, insert one.
 *
 * NOTE: This controller does NOT call OpenAI. It only reads from and writes
 *       to the [Chatbot] SQL Server table via LuxeGroomDbContext.
 */

using Microsoft.AspNetCore.Mvc;
using LuxeGroom.Data;
using LuxeGroom.Data.Generated;

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class ChatbotController : Controller
    {
        private readonly LuxeGroomDbContext _context;

        public ChatbotController(LuxeGroomDbContext context)
        {
            _context = context;
        }

        // ── GET /Chatbot/Chatbot ──────────────────────────────────────
        // Loads the most recent record and pre-fills the form.
        public IActionResult Chatbot()
        {
            // Always load the most recently created record as the active configuration
            var model = _context.Chatbot
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefault();

            ViewBag.ChatbotModel = model;
            return View("~/Views/Private/Chatbot.cshtml");
        }

        // ── POST /Chatbot/Chatbot ─────────────────────────────────────
        // Updates the existing record, or inserts a new one if none exists.
        [HttpPost]
        public IActionResult Chatbot(string instructions, string data)
        {
            if (string.IsNullOrWhiteSpace(instructions) || string.IsNullOrWhiteSpace(data))
            {
                ViewBag.Error        = "Both fields are required.";
                ViewBag.ChatbotModel = _context.Chatbot
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefault();
                return View("~/Views/Private/Chatbot.cshtml");
            }

            var existing = _context.Chatbot
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefault();

            if (existing != null)
            {
                // UPDATE the existing record
                existing.Instructions = instructions.Trim();
                existing.Data         = data.Trim();
                existing.CreatedDate  = DateTime.Now;
            }
            else
            {
                // INSERT first record if table is empty
                _context.Chatbot.Add(new Chatbot
                {
                    Instructions = instructions.Trim(),
                    Data         = data.Trim()
                });
            }

            _context.SaveChanges();

            ViewBag.Success      = "Saved successfully.";
            ViewBag.ChatbotModel = _context.Chatbot
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefault();

            return View("~/Views/Private/Chatbot.cshtml");
        }
    }
}
