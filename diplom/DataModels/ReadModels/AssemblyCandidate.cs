using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.ReadModels
{
    public class AssemblyCandidate
    {
        public string Sequence { get; set; } = string.Empty;
        public List<int> Qualities { get; set; } = new();

        public List<string> Path { get; set; } = new();
        public List<OverlapInfo> Overlaps { get; set; } = new();

        public int TotalScore => Overlaps.Sum(o => o.Score);
        public int TotalOverlap => Overlaps.Sum(o => o.OverlapLen);
        public double MinRate => Overlaps.Count == 0 ? 0 : Overlaps.Min(o => o.MatchRate);
    }
}
