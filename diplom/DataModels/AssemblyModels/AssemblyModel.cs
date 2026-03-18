using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.AssemblyModels
{
    public class AssemblyModel
    {
        public int Id { get; set; }
        [Required]
        public int ProjectId { get; set; }
        [Required]
        public string ConsensusSequence { get; set; } = string.Empty;
        [Required]
        public int ConsensusLength { get; set; }

        //public string? LowConfidenceRegionsJson { get; set; }
        //public string? ParametersJson { get; set; }
        //public string? MetricsJson { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
