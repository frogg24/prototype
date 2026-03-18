using Database.Models;
using DataModels.UserModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Implements
{
    public class UserStorage
    {
        public async Task<List<UserModel>> GetFullList()
        {
            using var context = new Database();
            return await context.Users.Select(x => x.GetViewModel).ToListAsync();
        }
        public async Task<List<UserModel>> GetFilteredList(UserSearchModel model)
        {
            using var context = new Database();

            var query = context.Users.AsQueryable();

            if (model.Id.HasValue)
            {
                query = query.Where(x => x.Id == model.Id.Value);
            }
            if (!string.IsNullOrEmpty(model.Username))
            {
                query = query.Where(x => x.Username.Equals(model.Username));
            }
            if (!string.IsNullOrEmpty(model.Email))
            {
                query = query.Where(x => x.Email.Equals(model.Email));
            }
            //TODO: make filter by created date

            var result = await query.Select(x => x.GetViewModel).ToListAsync();
            return result;
        }
        public async Task<UserModel?> GetElement(UserSearchModel model)
        {
            using var context = new Database();

            if (model.Id.HasValue)
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
                return user?.GetViewModel;
            }

            if (!string.IsNullOrEmpty(model.Username))
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Username.Equals(model.Username));
                return user?.GetViewModel;
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Email.Equals(model.Email));
                return user?.GetViewModel;
            }
            //TODO: make filter by created date
            return null;
        }

        public async Task<UserModel?> Insert(UserModel model)
        {
            var user = User.Create(model);
            if (user == null)
            {
                return null;
            }
            using var context = new Database();
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            return user.GetViewModel;
        }

        public async Task<UserModel?> Update(UserModel model)
        {
            using var context = new Database();
            var User = await context.Users.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (User == null)
            {
                return null;
            }
            User.Update(model);
            await context.SaveChangesAsync();
            return User.GetViewModel;
        }

        public async Task<UserModel?> Delete(int id)
        {
            using var context = new Database();
            var User = await context.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (User == null)
            {
                return null;
            }
            context.Users.Remove(User);
            await context.SaveChangesAsync();
            return User.GetViewModel;
        }
    }
}
