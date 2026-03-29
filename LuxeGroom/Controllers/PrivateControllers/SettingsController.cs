/*
 * SettingsController.cs
 * Handles Settings view for LuxeGroom.
 */

using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PrivateControllers
{
    public class SettingsController : Controller
    {
        // GET — validate session and render the Settings view
        public IActionResult Settings()
        {
            // Retrieve the logged-in user's session data
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            // Redirect to login if no active session exists
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Pass session data to the view
            ViewBag.Username = username;
            ViewBag.Role = role;

            // Render the Settings view
            return View("/Views/Private/Settings.cshtml");
        }
    }
}
