using Database.Implements;
using DataModels.ProjectModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class ProjectLogic
    {
        private readonly ProjectStorage _projectStorage;
        public ProjectLogic(ProjectStorage projectStorage)
        {
            _projectStorage = projectStorage;
        }

        public async Task<List<ProjectModel>?> ReadList(ProjectSearchModel? model)
        {
            var list = model == null
                ? await _projectStorage.GetFullList()
                : await _projectStorage.GetFilteredList(model);

            if (list == null)
            {
                return null;
            }

            return list;
        }

        public async Task<ProjectModel?> ReadElement(ProjectSearchModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var element = await _projectStorage.GetElement(model);

            if (element == null)
            {
                return null;
            }

            return element;
        }

        public async Task<bool> Create(ProjectModel model)
        {
            await CheckModelAsync(model);
            var result = await _projectStorage.Insert(model);
            return result != null;
        }

        public async Task<bool> Update(ProjectModel model)
        {
            await CheckModelAsync(model);
            var result = await _projectStorage.Update(model);
            return result != null;
        }

        public async Task<bool> Delete(int id)
        {
            var result = await _projectStorage.Delete(id);

            if (result == null)
            {
                return false;
            }

            return true;
        }

        private async Task CheckModelAsync(ProjectModel model, bool withParams = true)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!withParams)
            {
                return;
            }

            if (model.UserId <= 0)
            {
                throw new ArgumentException("некорректный ID пользователя", nameof(model.UserId));
            }
            
        }
    }
}
