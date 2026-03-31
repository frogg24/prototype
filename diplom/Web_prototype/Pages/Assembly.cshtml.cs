using DataModels.AssemblyModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Web_prototype.Pages
{
    public class AssemblyModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AssemblyModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public int ProjectId { get; set; }

        public DataModels.AssemblyModels.AssemblyModel? Assembly { get; private set; }
        public string? ErrorMessage { get; private set; }

        public async Task OnGetAsync(int projectId)
        {
            ProjectId = projectId;

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"api/assembly/project/{ProjectId}");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Не удалось загрузить сборку.";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            var list = JsonSerializer.Deserialize<List<DataModels.AssemblyModels.AssemblyModel>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assembly = list?.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).FirstOrDefault();

            if (Assembly == null)
            {
                ErrorMessage = "Сборка для проекта пока не найдена.";
            }
        }
    }
}