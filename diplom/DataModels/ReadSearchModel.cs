using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels
{
    public class ReadSearchModel
    {
        public int? Id { get; set; }
        public string? SampleName { get; set; }
        public string? InstrumentModel { get; set; }
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UserId { get; set; }
    }
}
