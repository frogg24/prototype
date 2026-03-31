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

        private (double matchrate, int overlapLen, int score) FindOverlap(PreparedRead leftRead, PreparedRead rightRead, int MinOverlap = 20, double minMatchRate = 0.3)
        {
            if (leftRead.PreparedSequence.Length < MinOverlap || rightRead.PreparedSequence.Length < MinOverlap)
            {
                Console.WriteLine("Маленькая длина цепочки");
                return (0, 0, 0);
            }

            string leftReadSeq = leftRead.PreparedSequence;
            string rightReadSeq = rightRead.PreparedSequence;
            int max = Math.Min(leftReadSeq.Length, rightReadSeq.Length);

            int trueLen = 0;
            double trueRate = 0.0;
            int trueSrore = 0;


            for (int len = max; len >= MinOverlap; len--)
            {
                int leftStart = leftReadSeq.Length - len;

                int matches = 0;
                int score = 0;

                for (int i = 0; i < len; i++)
                {
                    char a = leftReadSeq[leftStart + i];
                    char b = rightReadSeq[i];

                    if (a == b)
                    {
                        matches++;
                        score += 1;
                    }
                    else
                    {
                        //закоментил, чтобы некоторые неудачные варианты выводились
                        //score -= 1;
                    }

                }
                double matchRate = (double)matches / len;
                

                if (matchRate >= minMatchRate)
                {
                    if (score > trueSrore || (score == trueSrore && len > trueLen))
                    {
                        Console.WriteLine($"обновление мачрейта при len = {len}, новый мачрейт = {matchRate}");
                        trueLen = len;
                        trueRate = matchRate;
                        trueSrore = score;
                    }
                }
            }
            Console.WriteLine("Поиск перекорытия дошел до конца");
            return (trueRate, trueLen, trueSrore);
        }

        public async Task<AssemblyModel> OLC(int projectId)
        {
            List<ReadModel> reads = await _readStorage.GetFilteredList(new ReadSearchModel { ProjectId =  projectId });
            PrepareReads(reads);
            if (preparedReads.Count  <=1)
            {
                throw new Exception("not enough reads for OLC");
            }

            //частный случай для двух ридов
            if (preparedReads.Count == 2)
            {
                List<PairCandidate> candidates = BuildCandidates();
                PairCandidate best = PickBestCandidate(candidates);
                AssemblyModel assembly = BuildAssemblyFromCandidate(projectId, best);
                TrimByQ(assembly);
                assembly.ConsensusLength = assembly.ConsensusSequence.Length;
                assembly.UpdatedAt = DateTime.UtcNow;

                return assembly;
            }
            else
            {
                //in work
                return new AssemblyModel();
            }
        }

        private List<PairCandidate> BuildCandidates()
        {
            PreparedRead read1 = preparedReads[0];
            PreparedRead read2 = preparedReads[1];

            PreparedRead reversed2 = new PreparedRead
            {
                SourceId = read2.SourceId,
                OriginalSequence = read2.OriginalSequence,
                OriginalQualities = new List<int>(read2.OriginalQualities),
                WasReversed = true,
                PreparedSequence = ReverseRead(read2.PreparedSequence),
                PreparedQualities = ReverseQualities(new List<int>(read2.PreparedQualities)),
                LeftTrim = read2.LeftTrim,
                RightTrim = read2.RightTrim
            };
            Console.WriteLine("==================ПОПЫТКА1=====================");
            var f1f2 = FindOverlap(read1, read2);
            Console.WriteLine($"f1f2: len={f1f2.overlapLen}, rate={f1f2.matchrate:F3}, score={f1f2.score}");

            Console.WriteLine("==================ПОПЫТКА2=====================");
            var f1r2 = FindOverlap(read1, reversed2);
            Console.WriteLine($"f1r2: len={f1r2.overlapLen}, rate={f1r2.matchrate:F3}, score={f1r2.score}");

            Console.WriteLine("==================ПОПЫТКА3=====================");
            var f2f1 = FindOverlap(read2, read1);
            Console.WriteLine($"f2f1: len={f2f1.overlapLen}, rate={f2f1.matchrate:F3}, score={f2f1.score}");

            Console.WriteLine("==================ПОПЫТКА4=====================");
            var r2f1 = FindOverlap(reversed2, read1);
            Console.WriteLine($"r2f1: len={r2f1.overlapLen}, rate={r2f1.matchrate:F3}, score={r2f1.score}");

            return new List<PairCandidate>{
                new PairCandidate
                {
                    Name = "f1f2",
                    LeftRead = read1,
                    RightRead = read2,
                    RightWasReversed = false,
                    MatchRate = f1f2.matchrate,
                    OverlapLen = f1f2.overlapLen,
                    Score = f1f2.score
                },
                new PairCandidate
                {
                    Name = "f1r2",
                    LeftRead = read1,
                    RightRead = reversed2,
                    RightWasReversed = true,
                    MatchRate = f1r2.matchrate,
                    OverlapLen = f1r2.overlapLen,
                    Score = f1r2.score
                },
                new PairCandidate
                {
                    Name = "f2f1",
                    LeftRead = read2,
                    RightRead = read1,
                    RightWasReversed = false,
                    MatchRate = f2f1.matchrate,
                    OverlapLen = f2f1.overlapLen,
                    Score = f2f1.score
                },
                new PairCandidate
                {
                    Name = "r2f1",
                    LeftRead = reversed2,
                    RightRead = read1,
                    RightWasReversed = false,
                    MatchRate = r2f1.matchrate,
                    OverlapLen = r2f1.overlapLen,
                    Score = r2f1.score
                }
            };
        }

        private PairCandidate PickBestCandidate(List<PairCandidate> candidates)
        {
            return candidates.OrderByDescending(c => c.Score).ThenByDescending(c => c.OverlapLen).ThenByDescending(c => c.MatchRate).First();
        }
        private string ReverseRead(string seq)
        {
            //char[] result = seq.ToCharArray();
            //Array.Reverse(result);
            //return new string(result);

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

        private AssemblyModel BuildAssemblyFromCandidate(int projectId, PairCandidate candidate)
        {
            string leftSeq = candidate.LeftRead.PreparedSequence;
            string rightSeq = candidate.RightRead.PreparedSequence;

            List<int> leftQ = candidate.LeftRead.PreparedQualities;
            List<int> rightQ = candidate.RightRead.PreparedQualities;

            int overlapLen = candidate.OverlapLen;
            int leftUniqueLen = leftSeq.Length - overlapLen;

            StringBuilder consensus = new StringBuilder();
            List<int> consensusQ = new List<int>();

            //левая уникальная часть
            for (int i = 0; i < leftUniqueLen; i++)
            {
                consensus.Append(leftSeq[i]);
                consensusQ.Add(leftQ[i]);
            }

            //перекрытие
            for (int i = 0; i < overlapLen; i++)
            {
                char a = leftSeq[leftUniqueLen + i];
                char b = rightSeq[i];

                int qa = leftQ[leftUniqueLen + i];
                int qb = rightQ[i];

                if (a == b)
                {
                    consensus.Append(a);
                    consensusQ.Add(Math.Max(qa, qb));
                }
                else
                {
                    if (qa >= qb)
                    {
                        consensus.Append(a);
                        consensusQ.Add(qa);
                    }
                    else
                    {
                        consensus.Append(b);
                        consensusQ.Add(qb);
                    }
                }
            }

            //правая уникальная часть
            for (int i = overlapLen; i < rightSeq.Length; i++)
            {
                consensus.Append(rightSeq[i]);
                consensusQ.Add(rightQ[i]);
            }

            AssemblyModel assembly = new AssemblyModel
            {
                ProjectId = projectId,
                ConsensusSequence = consensus.ToString(),
                ConsensusLength = consensus.Length,
                QualityValuesJson = JsonSerializer.Serialize(consensusQ),
                CreatedAt = DateTime.UtcNow
            };

            return assembly;
        }
    }
}
