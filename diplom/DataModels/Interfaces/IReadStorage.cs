using DataModels.ReadModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.Interfaces
{
    public interface IReadStorage
    {
        Task<List<ReadModel>> GetFullList();
        Task<List<ReadModel>> GetFilteredList(ReadSearchModel model);
        Task<ReadModel?> GetElement(ReadSearchModel model);
        Task<ReadModel?> Insert(ReadModel model);
        Task<ReadModel?> Update(ReadModel model);
        Task<ReadModel?> Delete(int id);

    }
}
