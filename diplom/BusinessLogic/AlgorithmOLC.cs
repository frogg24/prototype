using Database.Implements;
using Database.Models;
using DataModels.AssemblyModels;
using DataModels.ReadModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class AlgorithmOLC
    {
        private readonly ReadStorage _readStorage;
        private readonly List<PreparedRead> preparedReads = new();
        private readonly ILogger<AlgorithmOLC> _logger;

        public AlgorithmOLC(ReadStorage readStorage, ILogger<AlgorithmOLC> logger)
        {
            _readStorage = readStorage;
            _logger = logger;
        }

        public void PrepareReads(List<ReadModel> reads)
        {
            _logger.LogInformation($"Prepare reads request received, readsCount={reads?.Count}");

            preparedReads.Clear();

            foreach (ReadModel read in reads)
            {
                if (string.IsNullOrWhiteSpace(read.Sequence) || string.IsNullOrWhiteSpace(read.QualityValuesJson))
                {
                    _logger.LogWarning($"Prepare read skipped, sequence or quality values are empty, readID={read.Id}");
                    continue;
                }

                try
                {
                    List<int> qualities = JsonSerializer.Deserialize<int[]>(read.QualityValuesJson)?.ToList() ?? new List<int>();
                    string sequence = read.Sequence.ToUpper();

                    if (qualities.Count != sequence.Length)
                    {
                        _logger.LogWarning($"Prepare read skipped, sequence length does not match quality values count, readID={read.Id}");
                        continue;
                    }

                    PreparedRead preparedRead = new PreparedRead
                    {
                        SourceId = read.Id,
                        OriginalSequence = sequence,
                        OriginalQualities = new List<int>(qualities),

                        WasReversed = false,
                        PreparedSequence = sequence,
                        PreparedQualities = new List<int>(qualities),

                        LeftTrim = 0,
                        RightTrim = 0
                    };

                    preparedReads.Add(preparedRead);
                    _logger.LogInformation($"Read prepared successfully, readID={read.Id}");
                }
                catch
                {
                    _logger.LogWarning($"Prepare read failed, invalid quality values json, readID={read.Id}");
                    continue;
                }
            }

            _logger.LogInformation($"Prepare reads success, preparedReadsCount={preparedReads.Count}");
        }

        private (int leftTrim, int rightTrim) TrimByQ(AssemblyModel assembly, int trimSearchLimit = 40, int window = 10, double minQ = 15.0)
        {
            _logger.LogInformation($"Trim by quality request received, assemblyID={assembly?.Id}, projectID={assembly?.ProjectId}");

            List<int> q = JsonSerializer.Deserialize<int[]>(assembly.QualityValuesJson)?.ToList() ?? new List<int>();
            int n = q.Count;

            if (n == 0 || string.IsNullOrEmpty(assembly.ConsensusSequence))
            {
                _logger.LogWarning($"Trim by quality skipped, quality values or consensus sequence are empty, assemblyID={assembly.Id}, projectID={assembly.ProjectId}");
                return (0, 0);
            }

            if (n != assembly.ConsensusSequence.Length)
            {
                _logger.LogWarning($"Trim by quality failed, consensus sequence length does not match quality values length, assemblyID={assembly.Id}, projectID={assembly.ProjectId}");
                throw new Exception("Consensus sequence length does not match quality values length");
            }

            if (n < window)
            {
                assembly.ConsensusLength = assembly.ConsensusSequence.Length;
                _logger.LogInformation($"Trim by quality skipped, consensus length less than window, assemblyID={assembly.Id}, projectID={assembly.ProjectId}");
                return (0, 0);
            }

            int leftTrim = 0;
            int rightTrim = 0;

            while (leftTrim < trimSearchLimit &&
                   leftTrim + window <= n &&
                   GetWindowAverage(q, leftTrim, window) < minQ)
            {
                leftTrim++;
            }

            while (rightTrim < trimSearchLimit &&
                   n - rightTrim - window >= 0 &&
                   GetWindowAverage(q, n - rightTrim - window, window) < minQ)
            {
                rightTrim++;
            }

            int newLength = n - leftTrim - rightTrim;
            if (newLength <= 0)
            {
                _logger.LogWarning($"Trim by quality failed, trim removed entire consensus, assemblyID={assembly.Id}, projectID={assembly.ProjectId}");
                throw new Exception("Trim removed entire consensus");
            }

            assembly.ConsensusSequence = assembly.ConsensusSequence.Substring(leftTrim, newLength);
            q = q.Skip(leftTrim).Take(newLength).ToList();
            assembly.QualityValuesJson = JsonSerializer.Serialize(q);
            assembly.ConsensusLength = assembly.ConsensusSequence.Length;

            Console.WriteLine($"на основе качества обрезка слева: {leftTrim} и справа: {rightTrim}");
            _logger.LogInformation($"Trim by quality success, assemblyID={assembly.Id}, projectID={assembly.ProjectId}, leftTrim={leftTrim}, rightTrim={rightTrim}");

            return (leftTrim, rightTrim);
        }

        private double GetWindowAverage(List<int> qualities, int start, int window)
        {
            int sum = 0;
            for (int i = 0; i < window; i++)
            {
                sum += qualities[start + i];
            }

            return (double)sum / window;
        }

        private OverlapInfo FindOverlap(string leftSeq, string rightSeq, int minOverlap = 20, double minMatchRate = 0.6)
        {
            if (leftSeq.Length < minOverlap || rightSeq.Length < minOverlap)
            {
                return new OverlapInfo();
            }

            int max = Math.Min(leftSeq.Length, rightSeq.Length);

            OverlapInfo best = new OverlapInfo
            {
                MatchRate = 0,
                OverlapLen = 0,
                Score = int.MinValue
            };

            for (int len = max; len >= minOverlap; len--)
            {
                int leftStart = leftSeq.Length - len;
                int matches = 0;
                int score = 0;

                for (int i = 0; i < len; i++)
                {
                    char a = leftSeq[leftStart + i];
                    char b = rightSeq[i];

                    if (a == b)
                    {
                        matches++;
                        score++;
                    }
                    else
                    {
                        score--;
                    }
                }

                double rate = (double)matches / len;

                if (rate >= minMatchRate)
                {
                    if (score > best.Score || (score == best.Score && len > best.OverlapLen))
                    {
                        best.Score = score;
                        best.OverlapLen = len;
                        best.MatchRate = rate;
                    }
                }
            }

            if (best.Score == int.MinValue)
            {
                return new OverlapInfo();
            }

            return best;
        }

        public async Task<AssemblyModel> OLC(int projectId)
        {
            _logger.LogInformation($"OLC request received, projectID={projectId}");

            try
            {
                List<ReadModel> reads = await _readStorage.GetFilteredList(new ReadSearchModel
                {
                    ProjectId = projectId
                });

                PrepareReads(reads);

                if (preparedReads.Count <= 1)
                {
                    _logger.LogWarning($"OLC failed, not enough reads, projectID={projectId}, preparedReadsCount={preparedReads.Count}");
                    throw new Exception("not enough reads for OLC");
                }

                if (preparedReads.Count > 4)
                {
                    _logger.LogWarning($"OLC failed, too many reads, projectID={projectId}, preparedReadsCount={preparedReads.Count}");
                    throw new Exception("current algorithm supports up to 4 reads");
                }

                AssemblyCandidate? best = BuildBestCandidate(preparedReads);

                if (best == null)
                {
                    _logger.LogWarning($"OLC failed, no valid assembly candidate found, projectID={projectId}");
                    throw new Exception("no valid assembly candidate found");
                }

                AssemblyModel assembly = new AssemblyModel
                {
                    ProjectId = projectId,
                    ConsensusSequence = best.Sequence,
                    ConsensusLength = best.Sequence.Length,
                    QualityValuesJson = JsonSerializer.Serialize(best.Qualities),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var trimResult = TrimByQ(assembly);

                List<ReadPlacementModel> finalPlacements = best.Placements
                    .Select(p => new ReadPlacementModel
                    {
                        ReadId = p.ReadId,
                        Start = p.Start - trimResult.leftTrim,
                        End = p.End - trimResult.leftTrim,
                        LeftTrim = p.LeftTrim,
                        RightTrim = p.RightTrim,
                        WasReversed = p.WasReversed
                    })
                    .Where(p => p.End > 0 && p.Start < assembly.ConsensusLength)
                    .Select(p =>
                    {
                        if (p.Start < 0)
                        {
                            p.Start = 0;
                        }

                        if (p.End > assembly.ConsensusLength)
                        {
                            p.End = assembly.ConsensusLength;
                        }

                        return p;
                    })
                    .Where(p => p.Start < p.End)
                    .ToList();

                assembly.TraceDataJson = JsonSerializer.Serialize(finalPlacements);
                assembly.ConsensusLength = assembly.ConsensusSequence.Length;
                assembly.UpdatedAt = DateTime.UtcNow;

                Console.WriteLine("BEST PATH: " + string.Join(" -> ", best.Path));
                Console.WriteLine($"TOTAL SCORE: {best.TotalScore}");
                Console.WriteLine($"TOTAL OVERLAP: {best.TotalOverlap}");
                Console.WriteLine($"MIN RATE: {best.MinRate:F3}");
                Console.WriteLine($"CONSENSUS LEN: {assembly.ConsensusLength}");

                _logger.LogInformation($"OLC success, projectID={projectId}, consensusLength={assembly.ConsensusLength}");

                return assembly;
            }
            catch (Exception ex)
            {
                _logger.LogError($"OLC failed, unexpected error, projectID={projectId}");
                throw;
            }
        }

        private string ReverseRead(string seq)
        {
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

            char[] result = new char[seq.Length];
            for (int i = 0; i < seq.Length; i++)
            {
                result[i] = Complement(seq[seq.Length - 1 - i]);
            }

            return new string(result);
        }

        private List<int> ReverseQualities(List<int> qualities)
        {
            int[] result = new int[qualities.Count];
            for (int i = 0; i < qualities.Count; i++)
            {
                result[i] = qualities[qualities.Count - 1 - i];
            }

            return result.ToList();
        }

        private PreparedRead CloneRead(PreparedRead read)
        {
            return new PreparedRead
            {
                SourceId = read.SourceId,
                OriginalSequence = read.OriginalSequence,
                OriginalQualities = new List<int>(read.OriginalQualities),
                WasReversed = read.WasReversed,
                PreparedSequence = read.PreparedSequence,
                PreparedQualities = new List<int>(read.PreparedQualities),
                LeftTrim = read.LeftTrim,
                RightTrim = read.RightTrim
            };
        }

        private PreparedRead MakeReversedCopy(PreparedRead read)
        {
            PreparedRead copy = CloneRead(read);
            copy.WasReversed = !copy.WasReversed;
            copy.PreparedSequence = ReverseRead(copy.PreparedSequence);
            copy.PreparedQualities = ReverseQualities(copy.PreparedQualities);
            return copy;
        }

        private static string FormatReadPath(PreparedRead read)
        {
            return $"{read.SourceId}:{(read.WasReversed ? "R" : "F")}";
        }

        private AssemblyCandidate CreateSeedCandidate(PreparedRead read)
        {
            return new AssemblyCandidate
            {
                Sequence = read.PreparedSequence,
                Qualities = new List<int>(read.PreparedQualities),
                Path = new List<string>
                {
                    FormatReadPath(read)
                },
                Overlaps = new List<OverlapInfo>(),
                Placements = new List<ReadPlacementModel>
                {
                    new ReadPlacementModel
                    {
                        ReadId = read.SourceId,
                        Start = 0,
                        End = read.PreparedSequence.Length,
                        LeftTrim = read.LeftTrim,
                        RightTrim = read.RightTrim,
                        WasReversed = read.WasReversed
                    }
                }
            };
        }

        private static AssemblyCandidate MergeCandidate(AssemblyCandidate leftContig, PreparedRead rightRead, OverlapInfo overlap)
        {
            var leftSeq = leftContig.Sequence;
            var rightSeq = rightRead.PreparedSequence;

            var leftQ = leftContig.Qualities;
            var rightQ = rightRead.PreparedQualities;

            var overlapLen = overlap.OverlapLen;
            var leftUniqueLen = leftSeq.Length - overlapLen;

            var merged = new StringBuilder();
            var mergedQ = new List<int>();

            for (var i = 0; i < leftUniqueLen; i++)
            {
                merged.Append(leftSeq[i]);
                mergedQ.Add(leftQ[i]);
            }

            for (var i = 0; i < overlapLen; i++)
            {
                var a = leftSeq[leftUniqueLen + i];
                var b = rightSeq[i];

                var qa = leftQ[leftUniqueLen + i];
                var qb = rightQ[i];

                if (a == b)
                {
                    merged.Append(a);
                    mergedQ.Add(Math.Max(qa, qb));
                }
                else if (qa >= qb)
                {
                    merged.Append(a);
                    mergedQ.Add(qa);
                }
                else
                {
                    merged.Append(b);
                    mergedQ.Add(qb);
                }
            }

            for (var i = overlapLen; i < rightSeq.Length; i++)
            {
                merged.Append(rightSeq[i]);
                mergedQ.Add(rightQ[i]);
            }

            var startBase = leftSeq.Length - overlapLen;
            var endBase = startBase + rightRead.PreparedSequence.Length;

            return new AssemblyCandidate
            {
                Sequence = merged.ToString(),
                Qualities = mergedQ,
                Path = new List<string>(leftContig.Path)
                {
                    FormatReadPath(rightRead)
                },
                Overlaps = new List<OverlapInfo>(leftContig.Overlaps)
                {
                    overlap
                },
                Placements = leftContig.Placements
                    .Select(p => new ReadPlacementModel
                    {
                        ReadId = p.ReadId,
                        Start = p.Start,
                        End = p.End,
                        LeftTrim = p.LeftTrim,
                        RightTrim = p.RightTrim,
                        WasReversed = p.WasReversed
                    })
                    .Append(new ReadPlacementModel
                    {
                        ReadId = rightRead.SourceId,
                        Start = startBase,
                        End = endBase,
                        LeftTrim = rightRead.LeftTrim,
                        RightTrim = rightRead.RightTrim,
                        WasReversed = rightRead.WasReversed
                    })
                    .ToList()
            };
        }

        private bool IsBetterCandidate(AssemblyCandidate current, AssemblyCandidate? best)
        {
            if (best == null)
            {
                return true;
            }

            if (current.TotalScore != best.TotalScore)
            {
                return current.TotalScore > best.TotalScore;
            }

            if (current.TotalOverlap != best.TotalOverlap)
            {
                return current.TotalOverlap > best.TotalOverlap;
            }

            if (Math.Abs(current.MinRate - best.MinRate) > 0.000001)
            {
                return current.MinRate > best.MinRate;
            }

            return current.Sequence.Length > best.Sequence.Length;
        }

        private IEnumerable<List<T>> GetReadsCombination<T>(List<T> items)
        {
            if (items.Count == 1)
            {
                yield return new List<T>(items);
                yield break;
            }

            for (int i = 0; i < items.Count; i++)
            {
                T current = items[i];
                List<T> rest = new List<T>(items);
                rest.RemoveAt(i);

                foreach (var perm in GetReadsCombination(rest))
                {
                    List<T> result = new List<T> { current };
                    result.AddRange(perm);
                    yield return result;
                }
            }
        }

        private IEnumerable<List<PreparedRead>> GetOrientationVariants(List<PreparedRead> reads)
        {
            if (reads.Count == 0)
            {
                yield return new List<PreparedRead>();
                yield break;
            }

            PreparedRead first = reads[0];
            List<PreparedRead> rest = reads.Skip(1).ToList();

            foreach (var restVariant in GetOrientationVariants(rest))
            {
                List<PreparedRead> directVariant = new List<PreparedRead>
                {
                    CloneRead(first)
                };
                directVariant.AddRange(restVariant.Select(CloneRead));
                yield return directVariant;

                List<PreparedRead> reversedVariant = new List<PreparedRead>
                {
                    MakeReversedCopy(first)
                };
                reversedVariant.AddRange(restVariant.Select(CloneRead));
                yield return reversedVariant;
            }
        }

        private AssemblyCandidate? BuildBestCandidate(List<PreparedRead> reads)
        {
            AssemblyCandidate? best = null;

            foreach (var combination in GetReadsCombination(reads))
            {
                foreach (var orientedReads in GetOrientationVariants(combination))
                {
                    AssemblyCandidate candidate = CreateSeedCandidate(orientedReads[0]);
                    bool failed = false;

                    Console.WriteLine("попытка собрать:");
                    foreach (var read in orientedReads)
                    {
                        Console.WriteLine($"ID: {read.SourceId}, Was reversed: {read.WasReversed}");
                    }
                    Console.WriteLine("=============");

                    for (int i = 1; i < orientedReads.Count; i++)
                    {
                        OverlapInfo overlap = FindOverlap(candidate.Sequence, orientedReads[i].PreparedSequence);

                        if (overlap.OverlapLen == 0)
                        {
                            failed = true;
                            break;
                        }

                        candidate = MergeCandidate(candidate, orientedReads[i], overlap);
                    }

                    if (!failed && IsBetterCandidate(candidate, best))
                    {
                        best = candidate;
                    }
                }
            }

            if (best == null)
            {
                _logger.LogWarning($"Build best assembly candidate failed, valid candidate not found, readsCount={reads.Count}");
            }
            else
            {
                _logger.LogInformation($"Build best assembly candidate success, readsCount={reads.Count}, consensusLength={best.Sequence.Length}");
            }

            return best;
        }
    }
}