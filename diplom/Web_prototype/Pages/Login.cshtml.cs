using DataModels.UserModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Web_prototype.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public LoginRequestModel Input { get; set; } = new();
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/MyProjects");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Errors = x.Value!.Errors.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                foreach (var item in errors)
                {
                    Console.WriteLine(item.Field);
                    foreach (var err in item.Errors)
                    {
                        Console.WriteLine("  " + err);
                    }
                }

                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            var payload = JsonSerializer.Serialize(Input);

            var response = await client.PostAsync("api/user/login", new StringContent(payload, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, TryGetMessage(content) ?? "Ошибка входа");
                return Page();
            }

            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("user", out var userElement))
            {
                ModelState.AddModelError(string.Empty, "Не удалось получить данные пользователя");
                return Page();
            }

            var userId = userElement.TryGetProperty("id", out var idElement) ? idElement.GetInt32() : 0;
            var username = userElement.TryGetProperty("username", out var usernameElement) ? usernameElement.GetString() : null;
            var email = userElement.TryGetProperty("email", out var emailElement) ? emailElement.GetString() : null;

            if (userId <= 0 || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Ответ API не содержит корректные данные пользователя");
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToPage("/MyProjects");
        }

        private static string? TryGetMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(content);
                return document.RootElement.TryGetProperty("message", out var message)
                    ? message.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
