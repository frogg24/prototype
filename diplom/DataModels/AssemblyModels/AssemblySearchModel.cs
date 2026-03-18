using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.AssemblyModels
{
    public class AssemblySearchModel
    {
        public int? Id { get; set; }
        public int? ProjectId { get; set; }
        public string? ConsensusSequence { get; set; }
        public int? ConsensusLength { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
