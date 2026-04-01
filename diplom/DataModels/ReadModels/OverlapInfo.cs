using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.ReadModels
{
    public class OverlapInfo
    {
        public double MatchRate { get; set; }
        public int OverlapLen { get; set; }
        public int Score { get; set; }
    }
}
