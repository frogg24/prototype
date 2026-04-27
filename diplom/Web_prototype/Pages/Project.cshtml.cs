using DataModels.ProjectModels;
using DataModels.ReadModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Web_prototype.Pages
{
    [Authorize]
    public class ProjectPageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProjectPageModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public List<IFormFile> UploadedFiles { get; set; } = new();

        public ProjectModel? Project { get; private set; }
        public List<ReadModel> Reads { get; private set; } = new();
        public string? ErrorMessage { get; private set; }
        public string? SuccessMessage { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Id = id;
            await LoadProjectAsync();
            await LoadReadsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(int id)
        {
            Id = id;

            if (UploadedFiles.Count == 0 || UploadedFiles.All(f => f.Length == 0))
            {
                ErrorMessage = "Выберите хотя бы один .ab1 файл.";
                await LoadProjectAsync();
                await LoadReadsAsync();
                return Page();
            }

            foreach (var uploadedFile in UploadedFiles.Where(f => f.Length > 0))
            {
                if (!string.Equals(Path.GetExtension(uploadedFile.FileName), ".ab1", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "Поддерживаются только файлы .ab1.";
                    await LoadProjectAsync();
                    await LoadReadsAsync();
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

            var response = await client.PostAsync($"api/read/project/{Id}/upload", formData);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Ошибка при загрузке ридов.";
                await LoadProjectAsync();
                await LoadReadsAsync();
                return Page();
            }

            SuccessMessage = "Файлы успешно загружены.";
            await LoadProjectAsync();
            await LoadReadsAsync();
            return Page();
        }

        private async Task LoadProjectAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"api/project/{Id}");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage ??= "Проект не найден.";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            Project = JsonSerializer.Deserialize<ProjectModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }

        private async Task LoadReadsAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"api/read/project/{Id}");
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage ??= "Не удалось загрузить риды проекта.";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            Reads = JsonSerializer.Deserialize<List<ReadModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? new List<ReadModel>();
        }

        public async Task<IActionResult> OnPostAssembleAsync(int id)
        {
            Id = id;

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.PostAsync($"api/assembly/project/{Id}/run", null);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Ошибка при сборке: {errorText}";
                await LoadProjectAsync();
                await LoadReadsAsync();
                return Page();
            }

            return RedirectToPage("/Assembly", new { projectId = Id });
        }

        public async Task<IActionResult> OnPostDeleteReadAsync(int id, int readId)
        {
            Id = id;

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.DeleteAsync($"api/read/{readId}");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "   .";
                await LoadProjectAsync();
                await LoadReadsAsync();
                return Page();
            }

            SuccessMessage = " .";
            await LoadProjectAsync();
            await LoadReadsAsync();
            return Page();
        }
    }
}