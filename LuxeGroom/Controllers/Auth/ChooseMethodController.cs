/*
 * ChooseMethodController.cs
 * Step 2 of the Forgot Password flow for LuxeGroom.
 * Displays masked Gmail and Phone options for the user to choose from.
 */

using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.Auth
{
    public class ChooseMethodController : Controller
    {
        public IActionResult Index()
        {
            // Retrieve the username passed from Step 1
            string username = TempData["Username"]?.ToString();

            // Redirect back to Step 1 if username is missing
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "ForgotPassword");

            // Keep TempData alive for the next request
            TempData.Keep("Username");

            // Pass username and masked contact options to the view
            ViewBag.Username = username;
            ViewBag.MaskedGmail = TempData["MaskedGmail"]?.ToString();
            ViewBag.MaskedPhone = TempData["MaskedPhone"]?.ToString();

            // Render the ChooseMethod view
            return View("~/Views/Auth/ChooseMethod.cshtml");
        }
    }
}
