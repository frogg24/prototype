using DataModels.AssemblyModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.Interfaces
{
    public interface IAssemblyStorage
    {
        Task<List<AssemblyModel>> GetFullList();
        Task<List<AssemblyModel>> GetFilteredList(AssemblySearchModel model);
        Task<AssemblyModel?> GetElement(AssemblySearchModel model);
        Task<AssemblyModel?> Insert(AssemblyModel model);
        Task<AssemblyModel?> Update(AssemblyModel model);
        Task<AssemblyModel?> Delete(int id);
    }
}
