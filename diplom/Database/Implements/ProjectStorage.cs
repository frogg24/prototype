using Database.Models;
using DataModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Implements
{
    public class ProjectStorage
    {
        public async Task<List<ProjectModel>> GetFullList()
        {
            using var context = new Database();
            return await context.Projects.Select(x => x.GetViewModel).ToListAsync();
        }
        public async Task<List<ProjectModel>> GetFilteredList(ProjectSearchModel model)
        {
            using var context = new Database();

            var query = context.Projects.AsQueryable();

            if (model.Id.HasValue)
            {
                query = query.Where(x => x.Id == model.Id.Value);
            }
            if (model.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == model.UserId.Value);
            }
            if (!string.IsNullOrEmpty(model.Title))
            {
                query = query.Where(x => x.Title.Equals(model.Title));
            }

            //TODO: make filter by created and updated dates

            var result = await query.Select(x => x.GetViewModel).ToListAsync();
            return result;
        }
        public async Task<ProjectModel?> GetElement(ProjectSearchModel model)
        {
            using var context = new Database();

            if (model.Id.HasValue)
            {
                var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
                return project?.GetViewModel;
            }

            if (model.Id.HasValue)
            {
                var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
                return project?.GetViewModel;
            }

            if (!string.IsNullOrEmpty(model.Title))
            {
                var project = await context.Projects.FirstOrDefaultAsync(x => x.Title.Equals(model.Title));
                return project?.GetViewModel;
            }

            //TODO: make filter by created and updated dates
            return null;
        }

        public async Task<ProjectModel?> Insert(ProjectModel model)
        {
            var project = Project.Create(model);
            if (project == null)
            {
                return null;
            }
            using var context = new Database();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            return project.GetViewModel;
        }

        public async Task<ProjectModel?> Update(ProjectModel model)
        {
            using var context = new Database();
            var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (project == null)
            {
                return null;
            }
            project.Update(model);
            await context.SaveChangesAsync();
            return project.GetViewModel;
        }

        public async Task<UserModel?> Delete(int id)
        {
            using var context = new Database();
            var project = await context.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (project == null)
            {
                return null;
            }
            context.Users.Remove(project);
            await context.SaveChangesAsync();
            return project.GetViewModel;
        }
    }
}
