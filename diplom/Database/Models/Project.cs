using DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Project
    {
        public int Id { get; private set; }
        [Required]
        public int UserId { get; private set; }
        [Required]
        public string Title { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public static Project? Create(ProjectModel project)
        {
            if (project == null)
            {
                return null;
            }
            return new Project
            {
                Id = project.Id,
                UserId = project.UserId,
                Title = project.Title,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
            };
        }

        public void Update(ProjectModel project)
        {
            if (project == null)
            {
                return;
            }

            Id = project.Id;
            UserId = project.UserId;
            Title = project.Title;
            CreatedAt = project.CreatedAt;
            UpdatedAt = project.UpdatedAt;
        }

        public ProjectModel GetViewModel => new()
        {
            Id = Id,
            UserId = UserId,
            Title = Title,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }
}
