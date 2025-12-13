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
            return RedirectToPage("/Index");
        }

        [HttpPost]
        public IActionResult Upload()
        {
            // In a real application, this would handle file uploads and processing
            // For this prototype, just redirect to the assembly page
            return RedirectToPage("/Assembly");
        }

        public IActionResult MyProjects()
        {
            // This page should be accessible only to authenticated users
            // For this prototype, we'll just render the view
            return RedirectToPage("/MyProjects");
        }

        public IActionResult Assembly()
        {
            // This page should be accessible only to authenticated users
            // For this prototype, we'll just render the view
            return RedirectToPage("/Assembly");
        }
    }
}