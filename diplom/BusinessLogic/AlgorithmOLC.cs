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
                try{
                    preparedRead.SourceId = read.Id;
                    preparedRead.OriginalSequence = read.Sequence.ToUpper();
                    preparedRead.OriginalQualities = JsonSerializer.Deserialize<int[]>(read.QualityValuesJson).ToList();

                    preparedRead.WasReversed = false;
                    preparedRead.PreparedSequence = read.Sequence.ToUpper();
                    preparedRead.PreparedQualities = JsonSerializer.Deserialize<int[]>(read.QualityValuesJson).ToList();

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
            //обрезка
            TrimByQ();
        }

        private void TrimByQ(int trimSearchLimit = 40, int window = 10, double minQ = 15.0)
        {
            //TODO: в этом методе бедо по индексам и может вылететь исклчение, надо накинуть проверок и обернуть в трай
            foreach (PreparedRead read in preparedReads)
            {
                int n = read.PreparedQualities.Count;
                int leftTrim = 0;
                int rightTrim = 0;

                while (leftTrim < trimSearchLimit && GetWindowAverage(read.PreparedQualities, leftTrim, window) < minQ)
                {
                    leftTrim++;
                }
                while (rightTrim < trimSearchLimit && GetWindowAverage(read.PreparedQualities, n - rightTrim - window, window) < minQ)
                {
                    rightTrim++;
                }

                int newLength = n - leftTrim - rightTrim;
                if (newLength <= 0)
                {
                    // TODO: обработка невалидных ридов
                    continue;
                }
                read.PreparedSequence = read.PreparedSequence.Substring(leftTrim, newLength);
                read.PreparedQualities = read.PreparedQualities.Skip(leftTrim).Take(newLength).ToList();

                read.LeftTrim += leftTrim;
                read.RightTrim += rightTrim;
            }
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

        private (double matchrate, int overlapLen, int score) FindOverlap(PreparedRead leftRead, PreparedRead rightRead, int MinOverlap = 20, double minMatchRate = 0.5)
        {
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
                int mismatches = 0;
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
                        mismatches++;
                        score -= 1;
                    }
                }
                double matchRate = (double)matches / len;

                if (matchRate >= minMatchRate)
                {
                    if (score > trueSrore || (score == trueSrore && len > trueLen))
                    {
                        trueLen = len;
                        trueRate = matchRate;
                        trueSrore = score;
                    }
                }
            }
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
                PreparedRead reversed = new PreparedRead
                {
                    SourceId = preparedReads[1].SourceId,
                    OriginalSequence = preparedReads[1].OriginalSequence,
                    OriginalQualities = new List<int>(preparedReads[1].OriginalQualities),
                    WasReversed = true,
                    PreparedSequence = ReverseComplement(preparedReads[1].PreparedSequence),
                    PreparedQualities = ReverseQualities(preparedReads[1].PreparedQualities),
                    LeftTrim = preparedReads[1].LeftTrim,
                    RightTrim = preparedReads[1].RightTrim
                };

                var f1f2 = FindOverlap(preparedReads[0], preparedReads[1]);
                var f1r2 = FindOverlap(preparedReads[0], reversed);
                var f2f1 = FindOverlap(preparedReads[1], preparedReads[0]);
                var r2f1 = FindOverlap(reversed, preparedReads[0]);


            }
            else
            {
                //in work
            }


            //int[,] overlapLengths = new int[preparedReads.Count, preparedReads.Count];

            //нахождение перекрытий между каждыми двумя ридами
            //for (int i = 0; i < preparedReads.Count; i++)
            //{
            //    for (int j = 0; j < preparedReads.Count; j++)
            //    {
            //        if (i == j)
            //        {
            //            overlapLengths[i, j] = -1;
            //            continue;
            //        }

            //        overlapLengths[i, j] = GetOverlapLength(preparedReads[i].PreparedSequence, preparedReads[j].PreparedSequence);
            //    }
            //}

        }

        //private int GetOverlapLength(string left, string right, int minOverlap = 10, double minMatchRate = 0.9)
        //{
        //    int max = Math.Min(left.Length, right.Length);

        //    for (int len = max; len >= minOverlap; len--)
        //    {
        //        string leftSuffix = left.Substring(left.Length - len, len);
        //        string rightPrefix = right.Substring(0, len);

        //        int matches = 0;

        //        for (int i = 0; i < len; i++)
        //        {
        //            if (leftSuffix[i] == rightPrefix[i])
        //            {
        //                matches++;
        //            }
        //        }

        //        double matchRate = (double)matches / len;

        //        if (matchRate >= minMatchRate)
        //        {
        //            return len;
        //        }
        //    }

        //    return 0;
        //}

        private string ReverseComplement(string seq)
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
    }
}
