using Database.Models;
using DataModels.AssemblyModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Implements
{
    public class AssemblyStorage
    {
        public async Task<List<AssemblyModel>> GetFullList()
        {
            using var context = new Database();
            return await context.Assemblies.Select(x => x.GetViewModel).ToListAsync();
        }
        public async Task<List<AssemblyModel>> GetFilteredList(AssemblySearchModel model)
        {
            using var context = new Database();

            var query = context.Assemblies.AsQueryable();

            if (model.Id.HasValue)
            {
                query = query.Where(x => x.Id == model.Id.Value);
            }
            if (model.ProjectId.HasValue)
            {
                query = query.Where(x => x.ProjectId == model.ProjectId.Value);
            }
            //TODO: make filter by created date and update date (maybe)

            var result = await query.Select(x => x.GetViewModel).ToListAsync();
            return result;
        }
        public async Task<AssemblyModel?> GetElement(AssemblySearchModel model)
        {
            using var context = new Database();

            if (model.Id.HasValue)
            {
                var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
                return assembly?.GetViewModel;
            }

            if (model.ProjectId.HasValue)
            {
                var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.ProjectId == model.ProjectId.Value);
                return assembly?.GetViewModel;
            }
            //TODO: make filter by created date and notes (maybe again)
            return null;
        }

        public async Task<AssemblyModel?> Insert(AssemblyModel model)
        {
            var assembly = Assembly.Create(model);
            if (assembly == null)
            {
                return null;
            }
            using var context = new Database();
            await context.Assemblies.AddAsync(assembly);
            await context.SaveChangesAsync();
            return assembly.GetViewModel;
        }

        public async Task<AssemblyModel?> Update(AssemblyModel model)
        {
            using var context = new Database();
            var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (assembly == null)
            {
                return null;
            }
            assembly.Update(model);
            await context.SaveChangesAsync();
            return assembly.GetViewModel;
        }

        public async Task<AssemblyModel?> Delete(int id)
        {
            using var context = new Database();
            var assembly = await context.Assemblies.FirstOrDefaultAsync(x => x.Id == id);
            if (assembly == null)
            {
                return null;
            }
            context.Assemblies.Remove(assembly);
            await context.SaveChangesAsync();
            return assembly.GetViewModel;
        }
    }
}
