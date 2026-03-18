using DataModels.AssemblyModels;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Assembly
    {
        public int Id { get; private set; }
        [Required]
        public int ProjectId { get; private set; }
        [Required]
        public string ConsensusSequence { get; private set; } = string.Empty;
        [Required]
        public int ConsensusLength { get; private set; }
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public static Assembly? Create(AssemblyModel assembly)
        {
            if (assembly == null)
            {
                return null;
            }
            return new Assembly
            {
                Id = assembly.Id,
                ProjectId = assembly.ProjectId,
                ConsensusSequence = assembly.ConsensusSequence,
                ConsensusLength = assembly.ConsensusSequence.Length,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }

        public void Update(AssemblyModel assembly)
        {
            if (assembly == null)
            {
                return;
            }

            ProjectId = assembly.ProjectId;
            ConsensusSequence = assembly.ConsensusSequence;
            ConsensusLength = assembly.ConsensusSequence.Length;
            UpdatedAt = assembly.UpdatedAt; 
        }

        public AssemblyModel GetViewModel => new()
        {
            Id = Id,
            ProjectId = ProjectId,
            ConsensusSequence = ConsensusSequence,
            ConsensusLength = ConsensusLength,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }
}
