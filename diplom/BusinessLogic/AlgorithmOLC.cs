using Database.Implements;
using Database.Models;
using DataModels.AssemblyModels;
using DataModels.ReadModels;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class AlgorithmOLC
    {
        private readonly ReadStorage _readStorage;
        private List<PreparedRead> preparedReads = new();

        public AlgorithmOLC(ReadStorage readStorage)
        {
            _readStorage=readStorage;
        }
        public void PrepareReads(List<ReadModel> reads)
        {
            preparedReads.Clear();
            foreach (ReadModel read in reads)
            {
                if (string.IsNullOrWhiteSpace(read.Sequence) || string.IsNullOrWhiteSpace(read.QualityValuesJson))
                {
                    continue;
                }

                PreparedRead preparedRead = new PreparedRead();
                try
                {
                    List<int> temp = JsonSerializer.Deserialize<int[]>(read.QualityValuesJson).ToList();

                    preparedRead.SourceId = read.Id;
                    preparedRead.OriginalSequence = read.Sequence.ToUpper();
                    preparedRead.OriginalQualities = new List<int>(temp);

                    preparedRead.WasReversed = false;
                    preparedRead.PreparedSequence = read.Sequence.ToUpper();
                    preparedRead.PreparedQualities = new List<int>(temp);

                    preparedRead.LeftTrim = 0;
                    preparedRead.RightTrim = 0;
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (preparedRead.PreparedQualities.Count != preparedRead.PreparedSequence.Length)
                {
                    continue;
                }

                preparedReads.Add(preparedRead);
            }
        }

        private void TrimByQ(AssemblyModel assemmbly, int trimSearchLimit = 40, int window = 10, double minQ = 15.0)
        {
            List<int> q = JsonSerializer.Deserialize<int[]>(assemmbly.QualityValuesJson).ToList();
            int n = q.Count;
            int leftTrim = 0;
            int rightTrim = 0;

            while (leftTrim < trimSearchLimit && GetWindowAverage(q, leftTrim, window) < minQ)
            {
                leftTrim++;
            }
            while (rightTrim < trimSearchLimit && GetWindowAverage(q, n - rightTrim - window, window) < minQ)
            {
                rightTrim++;
            }

            Console.WriteLine($"на основе качества образка слева:{leftTrim} и справа:{rightTrim}");

            int newLength = n - leftTrim - rightTrim;
            
            assemmbly.ConsensusSequence = assemmbly.ConsensusSequence.Substring(leftTrim, newLength);
            q = q.Skip(leftTrim).Take(newLength).ToList();

            assemmbly.QualityValuesJson = JsonSerializer.Serialize(q);
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
            List<ReadModel> reads = await _readStorage.GetFilteredList(new ReadSearchModel
            {
                ProjectId = projectId
            });

            PrepareReads(reads);

            if (preparedReads.Count <= 1)
            {
                throw new Exception("not enough reads for OLC");
            }

            if (preparedReads.Count > 4)
            {
                throw new Exception("current algorithm supports up to 4 reads");
            }

            AssemblyCandidate? best = BuildBestCandidate(preparedReads);

            if (best == null)
            {
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

            TrimByQ(assembly);
            assembly.UpdatedAt = DateTime.UtcNow;

            Console.WriteLine("BEST PATH: " + string.Join(" -> ", best.Path));
            Console.WriteLine($"TOTAL SCORE: {best.TotalScore}");
            Console.WriteLine($"TOTAL OVERLAP: {best.TotalOverlap}");
            Console.WriteLine($"MIN RATE: {best.MinRate:F3}");
            Console.WriteLine($"CONSENSUS LEN: {assembly.ConsensusLength}");

            return assembly;
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

        private AssemblyCandidate CreateSeedCandidate(PreparedRead read)
        {
            return new AssemblyCandidate
            {
                Sequence = read.PreparedSequence,
                Qualities = new List<int>(read.PreparedQualities),
                Path = new List<string>
        {
            $"{read.SourceId}:{(read.WasReversed ? "R" : "F")}"
        },
                Overlaps = new List<OverlapInfo>()
            };
        }

        private AssemblyCandidate MergeCandidate(AssemblyCandidate leftContig, PreparedRead rightRead, OverlapInfo overlap)
        {
            string leftSeq = leftContig.Sequence;
            string rightSeq = rightRead.PreparedSequence;

            List<int> leftQ = leftContig.Qualities;
            List<int> rightQ = rightRead.PreparedQualities;

            int overlapLen = overlap.OverlapLen;
            int leftUniqueLen = leftSeq.Length - overlapLen;

            StringBuilder merged = new StringBuilder();
            List<int> mergedQ = new List<int>();

            // левая уникальная часть
            for (int i = 0; i < leftUniqueLen; i++)
            {
                merged.Append(leftSeq[i]);
                mergedQ.Add(leftQ[i]);
            }

            // overlap
            for (int i = 0; i < overlapLen; i++)
            {
                char a = leftSeq[leftUniqueLen + i];
                char b = rightSeq[i];

                int qa = leftQ[leftUniqueLen + i];
                int qb = rightQ[i];

                if (a == b)
                {
                    merged.Append(a);
                    mergedQ.Add(Math.Max(qa, qb));
                }
                else
                {
                    if (qa >= qb)
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
            }

            // правая уникальная часть
            for (int i = overlapLen; i < rightSeq.Length; i++)
            {
                merged.Append(rightSeq[i]);
                mergedQ.Add(rightQ[i]);
            }

            return new AssemblyCandidate
            {
                Sequence = merged.ToString(),
                Qualities = mergedQ,
                Path = new List<string>(leftContig.Path){
                    $"{rightRead.SourceId}:{(rightRead.WasReversed ? "R" : "F")}"
                },
                Overlaps = new List<OverlapInfo>(leftContig.Overlaps)
                {
                    overlap
                }
            };
        }

        private bool IsBetterCandidate(AssemblyCandidate current, AssemblyCandidate? best)
        {
            if (best == null)
                return true;

            if (current.TotalScore != best.TotalScore)
                return current.TotalScore > best.TotalScore;

            if (current.TotalOverlap != best.TotalOverlap)
                return current.TotalOverlap > best.TotalOverlap;

            if (Math.Abs(current.MinRate - best.MinRate) > 0.000001)
                return current.MinRate > best.MinRate;

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
                // вариант 1: первый рид как есть
                List<PreparedRead> directVariant = new List<PreparedRead>
                {
                    CloneRead(first)
                };
                directVariant.AddRange(restVariant.Select(CloneRead));
                yield return directVariant;

                // вариант 2: первый рид развернутый
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

                    Console.WriteLine($"попытка собрать:");
                    foreach (var read in orientedReads){
                        Console.WriteLine($"ID: {read.SourceId}, Was reversed: {read.WasReversed}");
                    }
                    Console.WriteLine($"=============");

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

            return best;
        }
    }
}
