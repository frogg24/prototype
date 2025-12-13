using GenomeAssemblyApp.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GenomeAssemblyApp.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View("/Login.cshtml");
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // In a real application, you would validate credentials against a database
            // For this prototype, we'll just redirect to the home page
            return RedirectToAction("MyProjects", "Home");
        }

        public IActionResult Register()
        {
            return RedirectToPage("/Register");
        }

        [HttpPost]
        public IActionResult Register(string email, string password, string confirmPassword)
        {
            // In a real application, you would validate and store the user
            // For this prototype, we'll just redirect to the login page
            return RedirectToPage("/Login");
        }

        public IActionResult Logout()
        {
            // For this prototype, just redirect to home
            return RedirectToPage("/Index");
        }
    }
}