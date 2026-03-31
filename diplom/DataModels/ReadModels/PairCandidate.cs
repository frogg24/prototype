using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.ReadModels
{
    public class PairCandidate
    {
        public PreparedRead LeftRead { get; set; } = null!;
        public PreparedRead RightRead { get; set; } = null!;
        public bool RightWasReversed { get; set; }

        public double MatchRate { get; set; }
        public int OverlapLen { get; set; }
        public int Score { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
