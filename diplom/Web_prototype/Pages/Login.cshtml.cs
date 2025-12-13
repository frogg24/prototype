using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web_prototype.Pages
{
    public class LoginModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost(string email, string password)
        {
            return RedirectToPage("/MyProjects");
        }
    }
}
