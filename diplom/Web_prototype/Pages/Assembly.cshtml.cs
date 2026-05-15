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
                ErrorMessage = "Сборка для проекта не найдена.";
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
                var rawPeakLocations = ParseIntArray(read.PeakLocationsJson);
                var orientedPeakLocations = OrientPeakLocations(rawPeakLocations, orientedTraces.TraceLength, placement.WasReversed);
                var visibleLength = Math.Max(0, placement.End - placement.Start);

                var clipped = ClipVisibleData(
                    orientedSequence,
                    orientedTraces,
                    orientedPeakLocations,
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
                    },
                    PeakLocations = clipped.PeakLocations,
                    UsesPeakLocations = clipped.UsesPeakLocations
                });
            }

            var viewer = new ViewerDto
            {
                ConsensusSequence = assembly.ConsensusSequence ?? string.Empty,
                ConsensusLength = assembly.ConsensusLength,
                ConsensusQualities = ParseIntArray(assembly.QualityValuesJson),
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

        private static int[] OrientPeakLocations(int[] peakLocations, int traceLength, bool wasReversed)
        {
            if (peakLocations == null || peakLocations.Length == 0)
            {
                return Array.Empty<int>();
            }

            return wasReversed
                ? ChromatogramGeometry.ReversePeakLocations(peakLocations, traceLength)
                : peakLocations;
        }

        private static (string Sequence, TraceChannels Traces, int[] PeakLocations, bool UsesPeakLocations) ClipVisibleData(
            string sequence,
            TraceChannels traces,
            int[] peakLocations,
            int visibleLength,
            int start,
            int end,
            int consensusLength)
        {
            sequence ??= string.Empty;
            if (sequence.Length == 0)
            {
                return (string.Empty, new TraceChannels(), Array.Empty<int>(), false);
            }

            visibleLength = Math.Max(0, Math.Min(visibleLength, sequence.Length));
            if (visibleLength == 0)
            {
                return (string.Empty, new TraceChannels(), Array.Empty<int>(), false);
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
                return (string.Empty, new TraceChannels(), Array.Empty<int>(), false);
            }

            var visibleSequence = sequence.Substring(leftClipBases, keepLength);

            var traceLength = traces.TraceLength;
            var rawBaseCount = CountNonGapBases(sequence, 0, sequence.Length);
            var rawFromBase = CountNonGapBases(sequence, 0, leftClipBases);
            var rawToBase = CountNonGapBases(sequence, 0, leftClipBases + keepLength);
            var window = ChromatogramGeometry.SliceByPeakLocations(
                peakLocations,
                traceLength,
                rawBaseCount,
                rawFromBase,
                rawToBase);

            return (
                visibleSequence,
                new TraceChannels
                {
                    A = SliceTraceBySamples(traces.A, window.StartSample, window.EndSample),
                    C = SliceTraceBySamples(traces.C, window.StartSample, window.EndSample),
                    G = SliceTraceBySamples(traces.G, window.StartSample, window.EndSample),
                    T = SliceTraceBySamples(traces.T, window.StartSample, window.EndSample)
                },
                window.LocalPeakLocations,
                window.UsesPeakLocations);
        }

        private static int CountNonGapBases(string sequence, int start, int end)
        {
            if (string.IsNullOrEmpty(sequence))
            {
                return 0;
            }

            var safeStart = Math.Clamp(start, 0, sequence.Length);
            var safeEnd = Math.Clamp(end, safeStart, sequence.Length);
            var count = 0;

            for (var i = safeStart; i < safeEnd; i++)
            {
                if (sequence[i] != '-' && sequence[i] != '.')
                {
                    count++;
                }
            }

            return count;
        }

        private static int[] SliceTraceBySamples(int[] values, int startSampleInclusive, int endSampleExclusive)
        {
            if (values == null || values.Length == 0)
            {
                return Array.Empty<int>();
            }

            var safeStart = Math.Clamp(startSampleInclusive, 0, values.Length);
            var safeEnd = Math.Clamp(endSampleExclusive, safeStart, values.Length);
            return values.Skip(safeStart).Take(safeEnd - safeStart).ToArray();
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

        private static int[] ParseIntArray(string? json)
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

        private async Task<DataModels.AssemblyModels.AssemblyModel?> LoadLatestAssemblyAsync(int projectId)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            var assemblyResponse = await client.GetAsync($"api/assembly/project/{projectId}");
            if (!assemblyResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var assemblyJson = await assemblyResponse.Content.ReadAsStringAsync();
            var assemblies = JsonSerializer.Deserialize<List<DataModels.AssemblyModels.AssemblyModel>>(assemblyJson, ApiJsonOptions);

            return assemblies?
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefault();
        }

        private static string WrapFastaSequence(string sequence, int lineLength = 80)
        {
            if (string.IsNullOrWhiteSpace(sequence))
            {
                return string.Empty;
            }

            sequence = sequence.Trim().ToUpperInvariant();
            var sb = new StringBuilder();

            for (int i = 0; i < sequence.Length; i += lineLength)
            {
                int len = Math.Min(lineLength, sequence.Length - i);
                sb.AppendLine(sequence.Substring(i, len));
            }

            return sb.ToString();
        }

        public async Task<IActionResult> OnGetDownloadFastaAsync(int projectId)
        {
            var assembly = await LoadLatestAssemblyAsync(projectId);

            if (assembly == null || string.IsNullOrWhiteSpace(assembly.ConsensusSequence))
            {
                return NotFound("Assembly consensus not found");
            }

            var header = $">local|assembly_{assembly.Id}|project_{projectId} consensus length={assembly.ConsensusLength}";
            var fastaBody = WrapFastaSequence(assembly.ConsensusSequence);
            var fastaText = $"{header}\n{fastaBody}";

            var bytes = Encoding.UTF8.GetBytes(fastaText);
            var fileName = $"project_{projectId}_assembly_{assembly.Id}.fasta";

            return File(bytes, "text/plain", fileName);
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
            public int[] PeakLocations { get; set; } = Array.Empty<int>();
            public bool UsesPeakLocations { get; set; }
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
            public int TraceLength => new[] { A.Length, C.Length, G.Length, T.Length }.DefaultIfEmpty(0).Max();
        }
        public class SaveConsensusRequest
        {
            public int AssemblyId { get; set; }
            public int ProjectId { get; set; }
            public string ConsensusSequence { get; set; } = string.Empty;
        }
    }
}