using DataModels.ProjectModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Web_prototype.Pages
{
    [Authorize]
    public class MyProjectsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MyProjectsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public string NewProjectTitle { get; set; } = string.Empty;

        public List<ProjectModel> Projects { get; private set; } = new();
        public string? ErrorMessage { get; private set; }
        public string? SuccessMessage { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadProjectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(NewProjectTitle))
            {
                ErrorMessage = "Введите название проекта.";
                await LoadProjectsAsync();
                return Page();
            }

            var model = new DataModels.ProjectModels.ProjectModel
            {
                UserId = userId,
                Title = NewProjectTitle.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var client = _httpClientFactory.CreateClient("ApiClient");
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/project", content);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Не удалось создать проект.";
                await LoadProjectsAsync();
                return Page();
            }

            SuccessMessage = "Проект создан.";
            NewProjectTitle = string.Empty;
            await LoadProjectsAsync();
            return Page();
        }

        private async Task LoadProjectsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                Projects = new List<ProjectModel>();
                return;
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"api/project/user/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage ??= "Не удалось загрузить список проектов.";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            Projects = JsonSerializer.Deserialize<List<ProjectModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? new List<ProjectModel>();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var userId) ? userId : -1;
        }
    }
}
