using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.ProjectModels
{
    public class ProjectSearchModel
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public string? Title { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
