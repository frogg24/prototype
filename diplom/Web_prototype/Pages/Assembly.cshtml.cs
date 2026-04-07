using DataModels.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace Web_prototype.Pages
{
    public class AssemblyModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private static readonly JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions ViewerJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AssemblyModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration=configuration;
        }

        [BindProperty(SupportsGet = true)]
        public int ProjectId { get; set; }

        public DataModels.AssemblyModels.AssemblyModel? Assembly { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string ViewerJson { get; private set; } = "null";

        public async Task OnGetAsync(int projectId)
        {
            ProjectId = projectId;

            var client = _httpClientFactory.CreateClient("ApiClient");

            var assemblyResponse = await client.GetAsync($"api/assembly/project/{ProjectId}");
            if (!assemblyResponse.IsSuccessStatusCode)
            {
                ErrorMessage = "Не удалось загрузить сборку.";
                return;
            }

            var assemblyJson = await assemblyResponse.Content.ReadAsStringAsync();
            var assemblies = JsonSerializer.Deserialize<List<DataModels.AssemblyModels.AssemblyModel>>(assemblyJson, ApiJsonOptions);

            Assembly = assemblies?
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefault();

            if (Assembly == null)
            {
                ErrorMessage = "Сборка для проекта пока не найдена.";
                return;
            }

            var readsResponse = await client.GetAsync($"api/read/project/{ProjectId}");
            if (!readsResponse.IsSuccessStatusCode)
            {
                ErrorMessage = "Сборка загружена, но не удалось загрузить риды проекта.";
                return;
            }

            var readsJson = await readsResponse.Content.ReadAsStringAsync();
            var reads = JsonSerializer.Deserialize<List<ReadModel>>(readsJson, ApiJsonOptions) ?? new List<ReadModel>();

            ViewerJson = BuildViewerJson(Assembly, reads);
        }

        private static string BuildViewerJson(DataModels.AssemblyModels.AssemblyModel assembly, List<ReadModel> reads)
        {
            var placements = ParsePlacements(assembly.TraceDataJson)
                .OrderBy(x => x.Start)
                .ThenBy(x => x.ReadId)
                .ToList();

            var readsById = reads.ToDictionary(x => x.Id, x => x);
            var tracks = new List<ViewerTrackDto>();

            foreach (var placement in placements)
            {
                if (!readsById.TryGetValue(placement.ReadId, out var read))
                {
                    continue;
                }

                var orientedSequence = placement.WasReversed
                    ? ReverseComplement(read.Sequence)
                    : (read.Sequence ?? string.Empty).ToUpperInvariant();

                var orientedTraces = OrientTraces(ParseTraceChannels(read.TraceDataJson), placement.WasReversed);
                var visibleLength = Math.Max(0, placement.End - placement.Start);

                var clipped = ClipVisibleData(
                    orientedSequence,
                    orientedTraces,
                    visibleLength,
                    placement.Start,
                    placement.End,
                    assembly.ConsensusLength);

                var safeVisibleLength = clipped.Sequence.Length;
                if (safeVisibleLength == 0)
                {
                    continue;
                }

                tracks.Add(new ViewerTrackDto
                {
                    ReadId = read.Id,
                    FileName = read.FileName,
                    Sequence = clipped.Sequence,
                    Start = placement.Start,
                    End = placement.Start + safeVisibleLength,
                    VisibleLength = safeVisibleLength,
                    WasReversed = placement.WasReversed,
                    LeftTrim = placement.LeftTrim,
                    RightTrim = placement.RightTrim,
                    Traces = new ViewerTraceDto
                    {
                        A = clipped.Traces.A,
                        C = clipped.Traces.C,
                        G = clipped.Traces.G,
                        T = clipped.Traces.T
                    }
                });
            }

            var viewer = new ViewerDto
            {
                ConsensusSequence = assembly.ConsensusSequence ?? string.Empty,
                ConsensusLength = assembly.ConsensusLength,
                ConsensusQualities = ParseQualityArray(assembly.QualityValuesJson),
                Tracks = tracks
            };

            return JsonSerializer.Serialize(viewer, ViewerJsonOptions);
        }

        private static List<PlacementDto> ParsePlacements(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<PlacementDto>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<PlacementDto>>(json, ApiJsonOptions) ?? new List<PlacementDto>();
            }
            catch
            {
                return new List<PlacementDto>();
            }
        }

        private static TraceChannels ParseTraceChannels(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new TraceChannels();
            }

            try
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, short[]>>(json, ApiJsonOptions)
                          ?? new Dictionary<string, short[]>(StringComparer.OrdinalIgnoreCase);

                int[] ReadChannel(string key)
                {
                    if (raw.TryGetValue(key, out var values) && values != null)
                    {
                        return values.Select(v => (int)v).ToArray();
                    }

                    var match = raw.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                    return match.Value?.Select(v => (int)v).ToArray() ?? Array.Empty<int>();
                }

                return new TraceChannels
                {
                    A = ReadChannel("A"),
                    C = ReadChannel("C"),
                    G = ReadChannel("G"),
                    T = ReadChannel("T")
                };
            }
            catch
            {
                return new TraceChannels();
            }
        }

        private static TraceChannels OrientTraces(TraceChannels traces, bool wasReversed)
        {
            if (!wasReversed)
            {
                return traces;
            }

            return new TraceChannels
            {
                A = ReverseArray(traces.T),
                C = ReverseArray(traces.G),
                G = ReverseArray(traces.C),
                T = ReverseArray(traces.A)
            };
        }

        private static (string Sequence, TraceChannels Traces) ClipVisibleData(
            string sequence,
            TraceChannels traces,
            int visibleLength,
            int start,
            int end,
            int consensusLength)
        {
            sequence ??= string.Empty;
            if (sequence.Length == 0)
            {
                return (string.Empty, new TraceChannels());
            }

            visibleLength = Math.Max(0, Math.Min(visibleLength, sequence.Length));
            if (visibleLength == 0)
            {
                return (string.Empty, new TraceChannels());
            }

            var leftClipBases = 0;
            var rightClipBases = 0;
            var totalClipBases = sequence.Length - visibleLength;

            if (totalClipBases > 0)
            {
                if (start == 0 && end < consensusLength)
                {
                    leftClipBases = totalClipBases;
                }
                else if (start > 0 && end == consensusLength)
                {
                    rightClipBases = totalClipBases;
                }
                else
                {
                    rightClipBases = totalClipBases;
                }
            }

            var keepLength = sequence.Length - leftClipBases - rightClipBases;
            if (keepLength <= 0)
            {
                return (string.Empty, new TraceChannels());
            }

            var visibleSequence = sequence.Substring(leftClipBases, keepLength);

            return (
                visibleSequence,
                new TraceChannels
                {
                    A = SliceTraceByBases(traces.A, sequence.Length, leftClipBases, leftClipBases + keepLength),
                    C = SliceTraceByBases(traces.C, sequence.Length, leftClipBases, leftClipBases + keepLength),
                    G = SliceTraceByBases(traces.G, sequence.Length, leftClipBases, leftClipBases + keepLength),
                    T = SliceTraceByBases(traces.T, sequence.Length, leftClipBases, leftClipBases + keepLength)
                });
        }

        private static int[] SliceTraceByBases(int[] values, int totalBases, int fromBaseInclusive, int toBaseExclusive)
        {
            if (values == null || values.Length == 0 || totalBases <= 0)
            {
                return Array.Empty<int>();
            }

            var safeFrom = Math.Clamp(fromBaseInclusive, 0, totalBases);
            var safeTo = Math.Clamp(toBaseExclusive, safeFrom, totalBases);

            var startIndex = (int)Math.Floor(values.Length * (safeFrom / (double)totalBases));
            var endIndex = (int)Math.Ceiling(values.Length * (safeTo / (double)totalBases));

            startIndex = Math.Clamp(startIndex, 0, values.Length);
            endIndex = Math.Clamp(endIndex, startIndex, values.Length);

            return values.Skip(startIndex).Take(endIndex - startIndex).ToArray();
        }

        private static string ReverseComplement(string sequence)
        {
            sequence ??= string.Empty;
            sequence = sequence.ToUpperInvariant();

            char Complement(char c) => c switch
            {
                'A' => 'T',
                'T' => 'A',
                'G' => 'C',
                'C' => 'G',
                'R' => 'Y',
                'Y' => 'R',
                'S' => 'S',
                'W' => 'W',
                'K' => 'M',
                'M' => 'K',
                'B' => 'V',
                'V' => 'B',
                'D' => 'H',
                'H' => 'D',
                'N' => 'N',
                _ => 'N'
            };

            var result = new char[sequence.Length];
            for (var i = 0; i < sequence.Length; i++)
            {
                result[i] = Complement(sequence[sequence.Length - 1 - i]);
            }

            return new string(result);
        }

        private static int[] ReverseArray(int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return Array.Empty<int>();
            }

            var result = new int[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[values.Length - 1 - i];
            }

            return result;
        }

        private static int[] ParseQualityArray(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<int>();
            }

            try
            {
                return JsonSerializer.Deserialize<int[]>(json, ApiJsonOptions) ?? Array.Empty<int>();
            }
            catch
            {
                return Array.Empty<int>();
            }
        }

        public async Task<IActionResult> OnPostSaveConsensusAsync([FromBody] SaveConsensusRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Пустой запрос" });
            }

            if (request.AssemblyId <= 0)
            {
                return BadRequest(new { message = "Некорректный assemblyId" });
            }

            if (string.IsNullOrWhiteSpace(request.ConsensusSequence))
            {
                return BadRequest(new { message = "Пустая consensus sequence" });
            }

            var normalized = request.ConsensusSequence.Trim().ToUpperInvariant();

            foreach (var ch in normalized)
            {
                if (!"ACGTNRYSWKMBDHV".Contains(ch))
                {
                    return BadRequest(new { message = $"Недопустимый символ: {ch}" });
                }
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            var payload = new DataModels.AssemblyModels.AssemblyModel
            {
                Id = request.AssemblyId,
                ProjectId = request.ProjectId,
                ConsensusSequence = normalized,
                ConsensusLength = normalized.Length
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync($"api/assembly/{request.AssemblyId}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    message = string.IsNullOrWhiteSpace(responseBody) ? "Ошибка при сохранении" : responseBody
                });
            }

            return new JsonResult(new
            {
                ok = true,
                consensusSequence = normalized,
                consensusLength = normalized.Length
            });
        }

        private sealed class ViewerDto
        {
            public string ConsensusSequence { get; set; } = string.Empty;
            public int ConsensusLength { get; set; }
            public int[] ConsensusQualities { get; set; } = Array.Empty<int>();
            public List<ViewerTrackDto> Tracks { get; set; } = new();
        }

        private sealed class ViewerTrackDto
        {
            public int ReadId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string Sequence { get; set; } = string.Empty;
            public int Start { get; set; }
            public int End { get; set; }
            public int VisibleLength { get; set; }
            public bool WasReversed { get; set; }
            public int LeftTrim { get; set; }
            public int RightTrim { get; set; }
            public ViewerTraceDto Traces { get; set; } = new();
        }

        private sealed class ViewerTraceDto
        {
            public int[] A { get; set; } = Array.Empty<int>();
            public int[] C { get; set; } = Array.Empty<int>();
            public int[] G { get; set; } = Array.Empty<int>();
            public int[] T { get; set; } = Array.Empty<int>();
        }

        private sealed class PlacementDto
        {
            public int ReadId { get; set; }
            public int Start { get; set; }
            public int End { get; set; }
            public int LeftTrim { get; set; }
            public int RightTrim { get; set; }
            public bool WasReversed { get; set; }
        }

        private sealed class TraceChannels
        {
            public int[] A { get; set; } = Array.Empty<int>();
            public int[] C { get; set; } = Array.Empty<int>();
            public int[] G { get; set; } = Array.Empty<int>();
            public int[] T { get; set; } = Array.Empty<int>();
        }
        public class SaveConsensusRequest
        {
            public int AssemblyId { get; set; }
            public int ProjectId { get; set; }
            public string ConsensusSequence { get; set; } = string.Empty;
        }
    }
}