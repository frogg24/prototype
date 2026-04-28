using DataModels.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModels.Interfaces
{
    public interface IUserStorage
    {
        Task<List<UserModel>> GetFullList();
        Task<List<UserModel>> GetFilteredList(UserSearchModel model);
        Task<UserModel?> GetElement(UserSearchModel model);
        Task<UserModel?> Insert(UserModel model);
        Task<UserModel?> Update(UserModel model);
        Task<UserModel?> Delete(int id);
    }
}
