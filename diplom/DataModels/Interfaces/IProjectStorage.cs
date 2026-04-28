using DataModels.ProjectModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.Interfaces
{
    public interface IProjectStorage
    {
        Task<List<ProjectModel>> GetFullList();
        Task<List<ProjectModel>> GetFilteredList(ProjectSearchModel model);
        Task<ProjectModel?> GetElement(ProjectSearchModel model);
        Task<ProjectModel?> Insert(ProjectModel model);
        Task<ProjectModel?> Update(ProjectModel model);
        Task<ProjectModel?> Delete(int id);
    }
}
