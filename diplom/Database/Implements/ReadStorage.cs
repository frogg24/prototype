using Database.Models;
using DataModels.ReadModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Implements
{
    public class ReadStorage
    {
        public async Task<List<ReadModel>> GetFullList()
        {
            using var context = new Database();
            return await context.Reads.Select(x => x.GetViewModel).ToListAsync();
        }
        public async Task<List<ReadModel>> GetFilteredList(ReadSearchModel model)
        {
            using var context = new Database();

            var query = context.Reads.AsQueryable();

            if (model.Id.HasValue)
            {
                query = query.Where(x => x.Id == model.Id.Value);
            }
            if (model.ProjectId.HasValue)
            {
                query = query.Where(x => x.ProjectId == model.ProjectId.Value);
            }
            if (!string.IsNullOrEmpty(model.SampleName))
            {
                query = query.Where(x => x.SampleName.Equals(model.SampleName));
            }
            if (!string.IsNullOrEmpty(model.InstrumentModel))
            {
                query = query.Where(x => x.InstrumentModel.Equals(model.InstrumentModel));
            }
            //TODO: make filter by created date and notes (maybe)

            var result = await query.Select(x => x.GetViewModel).ToListAsync();
            return result;
        }
        public async Task<ReadModel?> GetElement(ReadSearchModel model)
        {
            using var context = new Database();

            if (model.Id.HasValue)
            {
                var read = await context.Reads.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
                return read?.GetViewModel;
            }

            if (model.ProjectId.HasValue)
            {
                var read = await context.Reads.FirstOrDefaultAsync(x => x.ProjectId == model.ProjectId.Value);
                return read?.GetViewModel;
            }


            if (!string.IsNullOrEmpty(model.SampleName))
            {
                var read = await context.Reads.FirstOrDefaultAsync(x => x.SampleName.Equals(model.SampleName));
                return read?.GetViewModel;
            }

            if (!string.IsNullOrEmpty(model.InstrumentModel))
            {
                var read = await context.Reads.FirstOrDefaultAsync(x => x.InstrumentModel.Equals(model.InstrumentModel));
                return read?.GetViewModel;
            }
            //TODO: make filter by created date and notes (maybe again)
            return null;
        }

        public async Task<ReadModel?> Insert(ReadModel model)
        {
            var read = Read.Create(model);
            if (read == null)
            {
                return null;
            }
            using var context = new Database();
            await context.Reads.AddAsync(read);
            await context.SaveChangesAsync();
            return read.GetViewModel;
        }

        public async Task<ReadModel?> Update(ReadModel model)
        {
            using var context = new Database();
            var read = await context.Reads.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (read == null)
            {
                return null;
            }
            read.Update(model);
            await context.SaveChangesAsync();
            return read.GetViewModel;
        }

        public async Task<ReadModel?> Delete(int id)
        {
            using var context = new Database();
            var read = await context.Reads.FirstOrDefaultAsync(x => x.Id == id);
            if (read == null)
            {
                return null;
            }
            context.Reads.Remove(read);
            await context.SaveChangesAsync();
            return read.GetViewModel;
        }
    }
}
