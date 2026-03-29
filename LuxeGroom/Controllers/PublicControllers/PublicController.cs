/*
 * PublicController.cs
 * Serves the public-facing LandingPage for LuxeGroom.
 */

using Microsoft.AspNetCore.Mvc;

namespace LuxeGroom.Controllers.PublicControllers
{
    public class PublicController : Controller
    {
        public IActionResult LandingPage()
        {
            return View();
        }
    }
}
