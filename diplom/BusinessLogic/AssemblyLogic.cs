using Database.Implements;
using DataModels.AssemblyModels;
using DataModels.enums;
using DataModels.ReadModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class AssemblyLogic
    {
        private readonly AssemblyStorage _assemblyStorage;

        public AssemblyLogic(AssemblyStorage assemblyStorage)
        {
            _assemblyStorage = assemblyStorage;
        }

        public async Task<AssemblyModel> MakeOLC(List<ReadModel> reads)
        {
            //TODO: make OLC
            return new AssemblyModel();
        }
        public async Task<List<AssemblyModel>> GetProjectAssemblysAsync(int projectId)
        {
            return await _assemblyStorage.GetFilteredList(new AssemblySearchModel { ProjectId = projectId });
        }

        public async Task<List<AssemblyModel>?> ReadList(AssemblySearchModel? model)
        {
            var list = model == null
                ? await _assemblyStorage.GetFullList()
                : await _assemblyStorage.GetFilteredList(model);

            if (list == null)
            {
                return null;
            }

            return list;
        }

        public async Task<AssemblyModel?> ReadElement(AssemblySearchModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var element = await _assemblyStorage.GetElement(model);

            if (element == null)
            {
                return null;
            }

            return element;
        }

        public async Task<bool> Create(AssemblyModel model)
        {
            await CheckModelAsync(model);
            var result = await _assemblyStorage.Insert(model);
            return result != null;
        }

        public async Task<bool> Update(AssemblyModel model)
        {
            await CheckModelAsync(model);
            var result = await _assemblyStorage.Update(model);
            return result != null;
        }

        public async Task<bool> Delete(int id)
        {
            var result = await _assemblyStorage.Delete(id);

            if (result == null)
            {
                return false;
            }

            return true;
        }

        private async Task CheckModelAsync(AssemblyModel model, bool withParams = true)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!withParams)
            {
                return;
            }

            if (model.ProjectId <=0 )
            {
                throw new ArgumentException("некорректный ID проекта", nameof(model.ProjectId));
            }
        }
    }
}
