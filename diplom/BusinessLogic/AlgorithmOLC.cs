using Database.Implements;
using Database.Models;
using DataModels.AssemblyModels;
using DataModels.ReadModels;
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
                    preparedRead.OriginalQualities = JsonSerializer.Deserialize<int[]>(read.QualityValuesJson);

                    preparedRead.WasReversed = false;
                    preparedRead.PreparedSequence = read.Sequence.ToUpper();
                    preparedRead.PreparedQualities = JsonSerializer.Deserialize<int[]>(read.QualityValuesJson);

                    //TODO: make a smart trim
                    preparedRead.LeftTrim = 0;
                    preparedRead.RightTrim = 0;
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (preparedRead.PreparedQualities.Length != preparedRead.PreparedSequence.Length)
                {
                    continue;
                }


                preparedReads.Add(preparedRead);
            }
        }
        public async Task<AssemblyModel> OLC(int projectId)
        {
            List<ReadModel> reads = await _readStorage.GetFilteredList(new ReadSearchModel { ProjectId =  projectId });
            PrepareReads(reads);

            int[,] overlapLengths = new int[preparedReads.Count, preparedReads.Count];

            //нахождение перекрытий между каждыми двумя ридами
            for (int i = 0; i < preparedReads.Count; i++)
            {
                for (int j = 0; j < preparedReads.Count; j++)
                {
                    if (i == j)
                    {
                        overlapLengths[i, j] = -1;
                        continue;
                    }

                    overlapLengths[i, j] = GetOverlapLength(preparedReads[i].PreparedSequence, preparedReads[j].PreparedSequence);
                }
            }

        }

        private int GetOverlapLength(string left, string right, int minOverlap = 10, double minMatchRate = 0.9)
        {
            int max = Math.Min(left.Length, right.Length);

            for (int len = max; len >= minOverlap; len--)
            {
                string leftSuffix = left.Substring(left.Length - len, len);
                string rightPrefix = right.Substring(0, len);

                int matches = 0;

                for (int i = 0; i < len; i++)
                {
                    if (leftSuffix[i] == rightPrefix[i])
                    {
                        matches++;
                    }
                }

                double matchRate = (double)matches / len;

                if (matchRate >= minMatchRate)
                {
                    return len;
                }
            }

            return 0;
        }
    }
}
