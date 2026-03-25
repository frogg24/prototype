using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.ReadModels
{
    public class PreparedRead
    {
        public int SourceId { get; set; }
        public string OriginalSequence { get; set; } = string.Empty;
        public string PreparedSequence { get; set; } = string.Empty;
        public List<int> OriginalQualities { get; set; }
        public List<int> PreparedQualities { get; set; }
        public bool WasReversed { get; set; }
        public int LeftTrim { get; set; }
        public int RightTrim { get; set; }
    }
}
