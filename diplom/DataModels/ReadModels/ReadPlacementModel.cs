using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.ReadModels
{
    public class ReadPlacementModel
    {
        public int ReadId { get; set; }

        // позиция рида в итоговой цепочке
        public int Start { get; set; }
        public int End { get; set; }

        // сколько было отрезано с краёв
        public int LeftTrim { get; set; }
        public int RightTrim { get; set; }

        // был ли рид развёрнут
        public bool WasReversed { get; set; }
    }
}
