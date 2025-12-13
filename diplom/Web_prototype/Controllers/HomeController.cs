using GenomeAssemblyApp.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GenomeAssemblyApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // This page should be accessible to all users, but in a real app,
            // you'd check authentication status
            return View("~/Views/Index.cshtml");
        }

        [HttpPost]
        public IActionResult Upload()
        {
            // In a real application, this would handle file uploads and processing
            // For this prototype, just redirect to the assembly page
            return RedirectToAction("Assembly");
        }

        public IActionResult MyProjects()
        {
            // This page should be accessible only to authenticated users
            // For this prototype, we'll just render the view
            return View("~/Views/MyProjects.cshtml");
        }

        public IActionResult Assembly()
        {
            // This page should be accessible only to authenticated users
            // For this prototype, we'll just render the view
            return View("~/Views/Assembly.cshtml");
        }
    }
}