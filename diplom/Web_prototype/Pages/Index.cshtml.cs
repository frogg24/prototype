using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace Web_prototype.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public List<IFormFile> UploadedFiles { get; set; } = new();

        public List<FilePreviewModel> FilePreviews { get; } = new();

        public string? ErrorMessage { get; private set; }
        public string? SuccessMessage { get; private set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFiles.Count == 0 || UploadedFiles.All(f => f.Length == 0))
            {
                ErrorMessage = "Выберите файлы для загрузки.";
                return Page();
            }

            foreach (var uploadedFile in UploadedFiles.Where(f => f.Length > 0))
            {
                if (!string.Equals(Path.GetExtension(uploadedFile.FileName), ".ab1", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "Поддерживается только формат .ab1. Удалите файлы с другим расширением.";
                    FilePreviews.Clear();
                    return Page();
                }
            }

            foreach (var uploadedFile in UploadedFiles.Where(f => f.Length > 0))
            {
                try
                {
                    var uploadedFileName = Path.GetFileName(uploadedFile.FileName);

                    await using var memoryStream = new MemoryStream();
                    await uploadedFile.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    var parsed = Ab1Parser.Parse(fileBytes);

                    var filePreview = new FilePreviewModel
                    {
                        UploadedFileName = uploadedFileName,
                        FileInfoMessage = BuildAb1Summary(parsed),
                        Sequence = parsed.Sequence,
                        SequencePreview = parsed.Sequence[..Math.Min(parsed.Sequence.Length, 120)],
                        FileContentHexDump = BuildHexDump(fileBytes.Take(512).ToArray())
                    };

                    FilePreviews.Add(filePreview);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при разборе файла {FileName}", uploadedFile.FileName);

                    FilePreviews.Add(new FilePreviewModel
                    {
                        UploadedFileName = uploadedFile.FileName,
                        FileInfoMessage = $"Ошибка разбора: {ex.Message}",
                        Sequence = string.Empty,
                        SequencePreview = string.Empty,
                        FileContentHexDump = string.Empty
                    });
                }
            }

            SuccessMessage = "Файл успешно загружен.";
            return Page();
        }

        private static string BuildAb1Summary(Ab1ReadResult parsed)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Signature: {parsed.Signature}");
            builder.AppendLine($"Version: {parsed.Version / 100.0:F2}");
            builder.AppendLine($"Directory entries: {parsed.Entries.Count}");

            if (!string.IsNullOrWhiteSpace(parsed.SampleName))
            {
                builder.AppendLine($"Sample name: {parsed.SampleName}");
            }

            if (!string.IsNullOrWhiteSpace(parsed.InstrumentModel))
            {
                builder.AppendLine($"Instrument: {parsed.InstrumentModel}");
            }

            if (!string.IsNullOrWhiteSpace(parsed.BaseOrder))
            {
                builder.AppendLine($"Base order: {parsed.BaseOrder}");
            }

            builder.AppendLine($"Sequence length: {parsed.Sequence.Length}");
            builder.AppendLine($"Quality values: {parsed.QualityValues.Count}");

            if (parsed.Traces.Count > 0)
            {
                builder.AppendLine("Trace channels:");
                foreach (var trace in parsed.Traces)
                {
                    builder.AppendLine($"  {trace.Key}: {trace.Value.Length} points");
                }
            }

            return builder.ToString();
        }

        private static string BuildHexDump(byte[] bytes)
        {
            var length = bytes.Length;
            var builder = new StringBuilder();

            for (var i = 0; i < length; i += 16)
            {
                builder.Append($"{i:X8}  ");

                for (var j = 0; j < 16; j++)
                {
                    if (i + j < length)
                    {
                        builder.Append($"{bytes[i + j]:X2} ");
                    }
                    else
                    {
                        builder.Append("   ");
                    }
                }

                builder.Append(" ");

                for (var j = 0; j < 16 && i + j < length; j++)
                {
                    var current = bytes[i + j];
                    builder.Append(current is >= 32 and <= 126 ? (char)current : '.');
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        public class FilePreviewModel
        {
            public string UploadedFileName { get; init; } = string.Empty;
            public string FileInfoMessage { get; init; } = string.Empty;
            public string Sequence { get; init; } = string.Empty;
            public string SequencePreview { get; init; } = string.Empty;
            public string FileContentHexDump { get; init; } = string.Empty;
        }
    }
}