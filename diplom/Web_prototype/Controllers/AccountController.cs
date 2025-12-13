using GenomeAssemblyApp.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GenomeAssemblyApp.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View("~/Views/Login.cshtml");
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // In a real application, you would validate credentials against a database
            // For this prototype, we'll just redirect to the home page
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View("~/Views/Register.cshtml");
        }

        [HttpPost]
        public IActionResult Register(string email, string password, string confirmPassword)
        {
            // In a real application, you would validate and store the user
            // For this prototype, we'll just redirect to the login page
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            // For this prototype, just redirect to home
            return RedirectToAction("Index", "Home");
        }
    }
}