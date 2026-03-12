using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Web_prototype.Pages
{
    [Authorize]
    public class MyProjectsModel : PageModel
    {
        public string Username => User.Identity?.Name ?? "Пользователь";
        public void OnGet()
        {
        }
    }
}
