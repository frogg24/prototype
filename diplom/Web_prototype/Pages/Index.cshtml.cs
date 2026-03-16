using DataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Web_prototype.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty]
        public List<IFormFile> UploadedFiles { get; set; } = new();

        public List<ReadModel> Reads { get; private set; } = new();
        public string? ErrorMessage { get; private set; }
        public string? SuccessMessage { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Login");
            }

            await LoadReadsAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToPage("/Login");
            }

            if (UploadedFiles.Count == 0 || UploadedFiles.All(f => f.Length == 0))
            {
                ErrorMessage = "Выберите хотя бы один .ab1 файл.";
                await LoadReadsAsync(userId);
                return Page();
            }

            foreach (var uploadedFile in UploadedFiles.Where(f => f.Length > 0))
            {
                if (!string.Equals(Path.GetExtension(uploadedFile.FileName), ".ab1", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "Поддерживаются только файлы .ab1.";
                    await LoadReadsAsync(userId);
                    return Page();
                }
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            using var formData = new MultipartFormDataContent();

            foreach (var file in UploadedFiles.Where(f => f.Length > 0))
            {
                await using var stream = file.OpenReadStream();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);

                var byteContent = new ByteArrayContent(memory.ToArray());
                byteContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                formData.Add(byteContent, "files", file.FileName);
            }

            var response = await client.PostAsync($"api/read/upload?userId={userId}", formData);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = TryGetMessage(content) ?? "Ошибка загрузки файлов.";
                await LoadReadsAsync(userId);
                return Page();
            }

            SuccessMessage = "Файлы загружены и обработаны.";
            await LoadReadsAsync(userId);
            return Page();
        }

        private async Task LoadReadsAsync(int userId)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"api/read/user/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage ??= "Не удалось загрузить риды пользователя.";
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            Reads = JsonSerializer.Deserialize<List<ReadModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? new List<ReadModel>();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var userId) ? userId : 0;
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
