using DataModels.UserModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace Web_prototype.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RegisterModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public RegisterRequestModel Input { get; set; } = new();

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

            var response = await client.PostAsync("api/user/register", new StringContent(payload, Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, TryGetMessage(content) ?? "Ошибка регистрации");
                return Page();
            }

            TempData["SuccessMessage"] = "Регистрация успешна. Теперь выполните вход.";
            return RedirectToPage("/Login");
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
